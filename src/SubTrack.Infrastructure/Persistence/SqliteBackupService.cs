using System.Diagnostics;
using System.Globalization;
using Microsoft.Data.Sqlite;

namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Servicio de respaldo consistente y restauración segura de la base SQLite para SubTrack.
/// Soporta bases activas en modo WAL, genera respaldos atómicos mediante VACUUM INTO,
/// valida la integridad estructural previa y garantiza recuperación automática ante fallos de restauración.
/// </summary>
public sealed class SqliteBackupService
{
    public const long ExpectedApplicationId = 1_398_096_468; // 'SUBT'

    private readonly SubTrackDatabaseOptions _databaseOptions;
    private readonly SubTrackBackupOptions _backupOptions;
    private readonly SqliteConnectionFactory _connectionFactory;

    public SqliteBackupService(
        SubTrackDatabaseOptions databaseOptions,
        SubTrackBackupOptions? backupOptions = null)
    {
        ArgumentNullException.ThrowIfNull(databaseOptions);

        _databaseOptions = databaseOptions.Normalize();
        _backupOptions = (backupOptions ?? SubTrackBackupOptions.CreateDefault(_databaseOptions))
            .Normalize(_databaseOptions);
        _connectionFactory = new SqliteConnectionFactory(_databaseOptions);
    }

    /// <summary>
    /// Crea un respaldo completo y consistente de la base de datos en caliente.
    /// Utiliza VACUUM INTO para consolidar el archivo principal y el log WAL de forma atómica.
    /// </summary>
    public async Task<BackupResult> CreateBackupAsync(
        string? destinationPath = null,
        CancellationToken cancellationToken = default)
    {
        EnsureDatabaseFileExists();

        var backupDir = _backupOptions.BackupDirectoryPath!;
        if (!Directory.Exists(backupDir))
        {
            Directory.CreateDirectory(backupDir);
        }

        var targetPath = !string.IsNullOrWhiteSpace(destinationPath)
            ? Path.GetFullPath(destinationPath)
            : Path.Combine(
                backupDir,
                $"{_backupOptions.BackupFilePrefix}_{DateTime.UtcNow:yyyyMMdd_HHmmss_fff}.db");

        var targetDir = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrWhiteSpace(targetDir) && !Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        // Usar archivo temporal para garantizar atomicidad y evitar archivos parciales ante cortes
        var tempTargetPath = targetPath + $".staging_{Guid.NewGuid():N}.tmp";

        var stopwatch = Stopwatch.StartNew();

        try
        {
            await using (var connection = await _connectionFactory.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                // Escapar comillas simples en la ruta para la instrucción VACUUM INTO
                var escapedPath = tempTargetPath.Replace("'", "''");

                await using var cmd = connection.CreateCommand();
                cmd.CommandText = $"VACUUM INTO '{escapedPath}';";
                cmd.CommandTimeout = Math.Max(60, _databaseOptions.BusyTimeoutMilliseconds / 1000);
                await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            stopwatch.Stop();

            // Validar la integridad del respaldo generado en el staging
            var verification = await VerifyBackupInternalAsync(tempTargetPath, cancellationToken)
                .ConfigureAwait(false);

            if (!verification.IsValid)
            {
                throw new SubTrackPersistenceException(
                    $"El respaldo generado en staging no superó la verificación de integridad: {verification.FailureReason}",
                    PersistenceFailureKind.BackupFailed,
                    tempTargetPath);
            }

            // Si el destino ya existía, eliminarlo para mover el staging
            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);
            }

            File.Move(tempTargetPath, targetPath);

            var fileInfo = new FileInfo(targetPath);

            return new BackupResult(
                BackupPath: targetPath,
                CreatedAt: DateTimeOffset.UtcNow,
                FileSizeBytes: fileInfo.Length,
                ApplicationId: verification.ApplicationId,
                UserVersion: verification.UserVersion,
                Duration: stopwatch.Elapsed,
                IntegrityCheckResult: verification.IntegrityStatus);
        }
        catch (Exception ex)
        {
            TryDeleteFile(tempTargetPath);

            if (ex is SubTrackPersistenceException)
            {
                throw;
            }

            if (ex is SqliteException sqlEx)
            {
                throw new SubTrackPersistenceException(
                    $"Error de SQLite al crear el respaldo: {sqlEx.Message}",
                    PersistenceFailureKind.BackupFailed,
                    targetPath,
                    sqlEx.SqliteExtendedErrorCode,
                    sqlEx);
            }

            throw new SubTrackPersistenceException(
                $"Error inesperado al crear el respaldo de la base de datos: {ex.Message}",
                PersistenceFailureKind.BackupFailed,
                targetPath,
                null,
                ex);
        }
    }

    /// <summary>
    /// Verifica la validez e integridad física y lógica de un archivo de respaldo.
    /// </summary>
    public async Task<BackupVerificationResult> VerifyBackupAsync(
        string backupPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(backupPath))
        {
            throw new ArgumentException("La ruta del respaldo es obligatoria.", nameof(backupPath));
        }

        var fullPath = Path.GetFullPath(backupPath);
        return await VerifyBackupInternalAsync(fullPath, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Restaura de forma segura una copia de seguridad en la ubicación activa de SubTrack.
    /// Valida el archivo previamente, conserva una copia de seguridad preventiva de la base actual
    /// y revierte automáticamente los cambios si ocurre cualquier fallo durante el proceso.
    /// </summary>
    public async Task<RestoreResult> RestoreBackupAsync(
        string sourceBackupPath,
        bool keepSafetyBackup = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sourceBackupPath))
        {
            throw new ArgumentException(
                "La ruta del archivo de respaldo es obligatoria.",
                nameof(sourceBackupPath));
        }

        var fullSourcePath = Path.GetFullPath(sourceBackupPath);

        // 1. Validar rigurosamente el respaldo antes de tocar la base activa
        var verification = await VerifyBackupInternalAsync(fullSourcePath, cancellationToken)
            .ConfigureAwait(false);

        if (!verification.IsValid)
        {
            throw new SubTrackPersistenceException(
                $"No se puede restaurar el archivo indicado porque no es un respaldo válido de SubTrack: {verification.FailureReason}",
                PersistenceFailureKind.InvalidBackup,
                fullSourcePath);
        }

        var activeDbPath = _databaseOptions.DatabasePath;
        var activeWalPath = activeDbPath + "-wal";
        var activeShmPath = activeDbPath + "-shm";

        var activeDbDir = Path.GetDirectoryName(activeDbPath);
        if (!string.IsNullOrWhiteSpace(activeDbDir) && !Directory.Exists(activeDbDir))
        {
            Directory.CreateDirectory(activeDbDir);
        }

        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff", CultureInfo.InvariantCulture);
        var safetyBackupPath = activeDbPath + $".pre_restore_{timestamp}.bak";
        var safetyWalPath = safetyBackupPath + "-wal";
        var safetyShmPath = safetyBackupPath + "-shm";

        var stopwatch = Stopwatch.StartNew();

        // 2. Cerrar y liberar todos los pools de conexión de SQLite para consolidar transacciones y permitir reemplazo
        SqliteConnection.ClearAllPools();

        var hadActiveDb = File.Exists(activeDbPath);
        var hadActiveWal = File.Exists(activeWalPath);
        var hadActiveShm = File.Exists(activeShmPath);

        try
        {
            // 3. Crear copia de seguridad preventiva de la base activa actual si existe
            if (hadActiveDb)
            {
                File.Copy(activeDbPath, safetyBackupPath, overwrite: true);

                if (File.Exists(activeWalPath))
                {
                    File.Copy(activeWalPath, safetyWalPath, overwrite: true);
                }

                if (File.Exists(activeShmPath))
                {
                    File.Copy(activeShmPath, safetyShmPath, overwrite: true);
                }
            }

            // 4. Copiar el respaldo en un archivo temporal adyacente a la base activa
            var stagingRestorePath = activeDbPath + $".restoring_{Guid.NewGuid():N}.tmp";
            File.Copy(fullSourcePath, stagingRestorePath, overwrite: true);

            // 5. Eliminar logs WAL/SHM antiguos para que la nueva base limpia no herede transacciones obsoletas
            TryDeleteFile(activeWalPath);
            TryDeleteFile(activeShmPath);

            // 6. Reemplazar la base activa con el archivo restaurado
            if (File.Exists(activeDbPath))
            {
                File.Delete(activeDbPath);
            }
            File.Move(stagingRestorePath, activeDbPath);

            // 7. Re-inicializar parámetros de la base restaurada (WAL, sincronismo, etc.) y verificar conexión
            var initializer = new SqliteDatabaseInitializer(_databaseOptions);
            await initializer.InitializeAsync(cancellationToken).ConfigureAwait(false);

            stopwatch.Stop();

            // 8. Si no se solicitó conservar la copia de seguridad preventiva, limpiarla
            if (!keepSafetyBackup && hadActiveDb)
            {
                TryDeleteFile(safetyBackupPath);
                TryDeleteFile(safetyWalPath);
                TryDeleteFile(safetyShmPath);
            }

            return new RestoreResult(
                RestoredDatabasePath: activeDbPath,
                SourceBackupPath: fullSourcePath,
                RestoredAt: DateTimeOffset.UtcNow,
                ApplicationId: verification.ApplicationId,
                UserVersion: verification.UserVersion,
                SafetyBackupPath: keepSafetyBackup && hadActiveDb ? safetyBackupPath : null,
                Duration: stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            // Reversión automática de emergencia: restaurar los archivos previos si existían
            SqliteConnection.ClearAllPools();

            if (hadActiveDb && File.Exists(safetyBackupPath))
            {
                try
                {
                    File.Copy(safetyBackupPath, activeDbPath, overwrite: true);

                    if (hadActiveWal && File.Exists(safetyWalPath))
                    {
                        File.Copy(safetyWalPath, activeWalPath, overwrite: true);
                    }

                    if (hadActiveShm && File.Exists(safetyShmPath))
                    {
                        File.Copy(safetyShmPath, activeShmPath, overwrite: true);
                    }
                }
                catch (Exception rollbackEx)
                {
                    throw new SubTrackPersistenceException(
                        $"Error crítico: Falló la restauración y no se pudo restaurar completamente la copia preventiva previa.\n" +
                        $"Error original: {ex.Message}\nError de rollback: {rollbackEx.Message}",
                        PersistenceFailureKind.RestoreFailed,
                        activeDbPath,
                        null,
                        ex);
                }
            }

            if (ex is SubTrackPersistenceException)
            {
                throw;
            }

            throw new SubTrackPersistenceException(
                $"Fallo durante la restauración. Se restauró exitosamente la copia previa de seguridad: {ex.Message}",
                PersistenceFailureKind.RestoreFailed,
                activeDbPath,
                null,
                ex);
        }
    }

    /// <summary>
    /// Lista los respaldos existentes en el directorio configurado, ordenados del más reciente al más antiguo.
    /// </summary>
    public Task<IReadOnlyList<BackupInfo>> ListBackupsAsync(CancellationToken cancellationToken = default)
    {
        var backupDir = _backupOptions.BackupDirectoryPath!;
        if (!Directory.Exists(backupDir))
        {
            return Task.FromResult<IReadOnlyList<BackupInfo>>(Array.Empty<BackupInfo>());
        }

        var directoryInfo = new DirectoryInfo(backupDir);
        var files = directoryInfo
            .GetFiles("*.db", SearchOption.TopDirectoryOnly)
            .OrderByDescending(f => f.CreationTimeUtc)
            .Select(f => new BackupInfo(
                FilePath: f.FullName,
                FileName: f.Name,
                CreatedAt: new DateTimeOffset(f.CreationTimeUtc),
                FileSizeBytes: f.Length))
            .ToList();

        return Task.FromResult<IReadOnlyList<BackupInfo>>(files);
    }

    /// <summary>
    /// Elimina respaldos antiguos que excedan el límite de retención especificado.
    /// </summary>
    public async Task<int> PruneBackupsAsync(int? keepCount = null, CancellationToken cancellationToken = default)
    {
        var limit = keepCount ?? _backupOptions.DefaultRetentionCount;
        if (limit < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(keepCount),
                "La cantidad de respaldos a conservar debe ser al menos 1.");
        }

        var backups = await ListBackupsAsync(cancellationToken).ConfigureAwait(false);
        if (backups.Count <= limit)
        {
            return 0;
        }

        var toDelete = backups.Skip(limit).ToList();
        var deletedCount = 0;

        foreach (var backup in toDelete)
        {
            try
            {
                if (File.Exists(backup.FilePath))
                {
                    File.Delete(backup.FilePath);
                    deletedCount++;
                }
            }
            catch
            {
                // Ignorar archivos en uso durante la poda
            }
        }

        return deletedCount;
    }

    private static async Task<BackupVerificationResult> VerifyBackupInternalAsync(
        string fullPath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(fullPath))
        {
            return new BackupVerificationResult(
                IsValid: false,
                BackupPath: fullPath,
                FileSizeBytes: 0,
                ApplicationId: 0,
                UserVersion: 0,
                IntegrityStatus: "file_not_found",
                FailureReason: "El archivo de respaldo no existe en la ruta indicada.",
                MigrationsCount: 0);
        }

        var fileInfo = new FileInfo(fullPath);
        if (fileInfo.Length == 0)
        {
            return new BackupVerificationResult(
                IsValid: false,
                BackupPath: fullPath,
                FileSizeBytes: 0,
                ApplicationId: 0,
                UserVersion: 0,
                IntegrityStatus: "empty_file",
                FailureReason: "El archivo de respaldo está vacío (0 bytes).",
                MigrationsCount: 0);
        }

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadOnly,
            Pooling = false
        }.ToString();

        try
        {
            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);

            // 1. Verificar application_id
            await using var appIdCmd = connection.CreateCommand();
            appIdCmd.CommandText = "PRAGMA application_id;";
            var appIdScalar = await appIdCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            var appId = Convert.ToInt64(appIdScalar, CultureInfo.InvariantCulture);

            if (appId != ExpectedApplicationId)
            {
                return new BackupVerificationResult(
                    IsValid: false,
                    BackupPath: fullPath,
                    FileSizeBytes: fileInfo.Length,
                    ApplicationId: appId,
                    UserVersion: 0,
                    IntegrityStatus: "foreign_database",
                    FailureReason: $"El archivo no pertenece a SubTrack (application_id={appId}, esperado={ExpectedApplicationId}).",
                    MigrationsCount: 0);
            }

            // 2. Verificar integridad física de SQLite
            await using var integrityCmd = connection.CreateCommand();
            integrityCmd.CommandText = "PRAGMA quick_check(1);";
            var integrityScalar = await integrityCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            var integrityStatus = integrityScalar?.ToString() ?? "unknown";

            if (!string.Equals(integrityStatus, "ok", StringComparison.OrdinalIgnoreCase))
            {
                return new BackupVerificationResult(
                    IsValid: false,
                    BackupPath: fullPath,
                    FileSizeBytes: fileInfo.Length,
                    ApplicationId: appId,
                    UserVersion: 0,
                    IntegrityStatus: integrityStatus,
                    FailureReason: $"La comprobación de integridad rápida de SQLite falló: {integrityStatus}",
                    MigrationsCount: 0);
            }

            // 3. Consultar user_version
            await using var userVerCmd = connection.CreateCommand();
            userVerCmd.CommandText = "PRAGMA user_version;";
            var userVerScalar = await userVerCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            var userVersion = Convert.ToInt32(userVerScalar, CultureInfo.InvariantCulture);

            // 4. Consultar cantidad de migraciones registradas si existe la tabla
            var migrationsCount = 0;
            await using var countCmd = connection.CreateCommand();
            countCmd.CommandText = @"
                SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='esquema_migraciones';";
            var tableExists = Convert.ToInt32(await countCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false)) > 0;

            if (tableExists)
            {
                await using var migCountCmd = connection.CreateCommand();
                migCountCmd.CommandText = "SELECT COUNT(*) FROM esquema_migraciones;";
                migrationsCount = Convert.ToInt32(await migCountCmd.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
            }

            return new BackupVerificationResult(
                IsValid: true,
                BackupPath: fullPath,
                FileSizeBytes: fileInfo.Length,
                ApplicationId: appId,
                UserVersion: userVersion,
                IntegrityStatus: "ok",
                FailureReason: null,
                MigrationsCount: migrationsCount);
        }
        catch (SqliteException sqlEx)
        {
            return new BackupVerificationResult(
                IsValid: false,
                BackupPath: fullPath,
                FileSizeBytes: fileInfo.Length,
                ApplicationId: 0,
                UserVersion: 0,
                IntegrityStatus: "sqlite_error",
                FailureReason: $"Error de SQLite al abrir o verificar el archivo: {sqlEx.Message}",
                MigrationsCount: 0);
        }
        catch (Exception ex)
        {
            return new BackupVerificationResult(
                IsValid: false,
                BackupPath: fullPath,
                FileSizeBytes: fileInfo.Length,
                ApplicationId: 0,
                UserVersion: 0,
                IntegrityStatus: "read_error",
                FailureReason: $"No se pudo leer el archivo de respaldo: {ex.Message}",
                MigrationsCount: 0);
        }
    }

    private void EnsureDatabaseFileExists()
    {
        if (!File.Exists(_databaseOptions.DatabasePath))
        {
            throw new SubTrackPersistenceException(
                $"No se puede respaldar porque la base de datos activa no existe en '{_databaseOptions.DatabasePath}'.",
                PersistenceFailureKind.CannotOpen,
                _databaseOptions.DatabasePath);
        }
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // Limpieza pasiva de archivos temporales
        }
    }
}
