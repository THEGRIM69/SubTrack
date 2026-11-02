using Microsoft.Data.Sqlite;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Tests.Infrastructure;

/// <summary>
/// Verifica las garantías de integridad del esquema de dominio (migración 0001).
/// Cada prueba intenta violar una restricción aprobada en las 75 decisiones de SubTrack 1.0
/// y comprueba que SQLite la rechaza.
/// </summary>
public sealed class DomainSchemaTests : IAsyncLifetime
{
    private const string Timestamp = "2026-10-25T00:00:00.000Z";
    private const string SuscripcionId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
    private const string CobroId = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";
    private const string CategoriaId = "11111111-1111-4111-8111-111111111111";

    private readonly string _tempDirectory;
    private readonly SubTrackDatabaseOptions _options;

    public DomainSchemaTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "SubTrack.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);

        _options = new SubTrackDatabaseOptions
        {
            DatabasePath = Path.Combine(_tempDirectory, "subtrack-schema-test.db"),
            BusyTimeoutMilliseconds = 2_000
        };
    }

    public async Task InitializeAsync()
    {
        var migrator = new SqliteDatabaseMigrator(_options);
        var result = await migrator.MigrateAsync();

        Assert.True(result.Success);
        Assert.Equal(OfficialMigrations.InitialSchemaVersion, result.FinalVersion);
    }

    public Task DisposeAsync()
    {
        SqliteConnection.ClearAllPools();

        try
        {
            if (Directory.Exists(_tempDirectory))
            {
                Directory.Delete(_tempDirectory, recursive: true);
            }
        }
        catch (IOException)
        {
            // Un archivo aún bloqueado no invalida el resultado de la prueba.
        }

        return Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Estructura del esquema
    // -------------------------------------------------------------------------

    [Fact]
    public async Task MigracionInicial_CreaTodasLasTablasDeDominio()
    {
        var expectedTables = new[]
        {
            "categorias_gasto",
            "suscripciones",
            "suscripcion_versiones",
            "cobros_programados",
            "pagos",
            "gastos",
            "gastos_historial",
            "ingresos_plantillas",
            "ingresos",
            "ingresos_historial",
            "saldo_inicial",
            "saldo_inicial_historial",
            "secuencias"
        };

        await using var connection = await OpenAsync();

        var tables = new List<string>();
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table';";
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tables.Add(reader.GetString(0));
            }
        }

        foreach (var table in expectedTables)
        {
            Assert.Contains(table, tables);
        }
    }

    [Fact]
    public async Task MigracionInicial_RegistraVersionUnoEnLaBase()
    {
        var migrator = new SqliteDatabaseMigrator(_options);

        Assert.Equal(OfficialMigrations.InitialSchemaVersion, await migrator.GetCurrentVersionAsync());

        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";

        var userVersion = Convert.ToInt32(await command.ExecuteScalarAsync());
        Assert.Equal(OfficialMigrations.InitialSchemaVersion, userVersion);
    }

    // -------------------------------------------------------------------------
    // F0-13 — Categorías predefinidas
    // -------------------------------------------------------------------------

    [Fact]
    public async Task MigracionInicial_SiembraLasCategoriasAprobadas()
    {
        await using var connection = await OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT nombre FROM categorias_gasto ORDER BY nombre;";

        var nombres = new List<string>();
        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            nombres.Add(reader.GetString(0));
        }

        Assert.Equal(new[] { "Alimentación", "Educación", "Salud", "Transporte" }, nombres);
    }

    // -------------------------------------------------------------------------
    // Convenciones de datos (F0-T12, F0-T30, F0-T33)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Suscripcion_RechazaIdentificadorQueNoSeaGuidMinusculas()
    {
        await using var connection = await OpenAsync();

        var exception = await Assert.ThrowsAsync<SqliteException>(
            () => InsertSuscripcionAsync(connection, id: "NO-ES-UN-GUID"));

        Assert.Contains("CHECK", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cobro_RechazaFechaInexistente()
    {
        await using var connection = await OpenAsync();
        await InsertSuscripcionAsync(connection, SuscripcionId);

        await Assert.ThrowsAsync<SqliteException>(
            () => InsertCobroAsync(connection, CobroId, SuscripcionId, fecha: "2026-02-30"));
    }

    [Fact]
    public async Task Gasto_RechazaImporteNoPositivo()
    {
        await using var connection = await OpenAsync();

        await Assert.ThrowsAsync<SqliteException>(
            () => InsertGastoAsync(connection, "cccccccc-cccc-4ccc-8ccc-cccccccccccc", monto: 0));
    }

    // -------------------------------------------------------------------------
    // F0-10 — Desactivación con fecha efectiva
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Suscripcion_RechazaDesactivacionSinFechaEfectiva()
    {
        await using var connection = await OpenAsync();

        var exception = await Assert.ThrowsAsync<SqliteException>(
            () => InsertSuscripcionAsync(connection, SuscripcionId, activa: 0, desactivadaEn: null));

        Assert.Contains("CHECK", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Suscripcion_RechazaSuscripcionActivaConFechaDeDesactivacion()
    {
        await using var connection = await OpenAsync();

        await Assert.ThrowsAsync<SqliteException>(
            () => InsertSuscripcionAsync(
                connection,
                SuscripcionId,
                activa: 1,
                desactivadaEn: "2026-10-20"));
    }

    [Fact]
    public async Task Suscripcion_PermiteDesactivacionConFechaEfectiva()
    {
        await using var connection = await OpenAsync();

        await InsertSuscripcionAsync(
            connection,
            SuscripcionId,
            activa: 0,
            desactivadaEn: "2026-10-20");

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT desactivada_en FROM suscripciones WHERE id = $id;";
        command.Parameters.AddWithValue("$id", SuscripcionId);

        Assert.Equal("2026-10-20", (string?)await command.ExecuteScalarAsync());
    }

    [Fact]
    public async Task Suscripcion_RechazaActivaSinProximoCobro()
    {
        await using var connection = await OpenAsync();

        var exception = await Assert.ThrowsAsync<SqliteException>(
            () => InsertSuscripcionAsync(connection, SuscripcionId, proximo: null, activa: 1));

        Assert.Contains("CHECK", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Suscripcion_PermiteInactivaSinProximoCobro()
    {
        await using var connection = await OpenAsync();

        await InsertSuscripcionAsync(
            connection,
            SuscripcionId,
            proximo: null,
            activa: 0,
            desactivadaEn: "2026-10-20");

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM suscripciones WHERE proximo_cobro IS NULL;";

        Assert.Equal(1L, Convert.ToInt64(await command.ExecuteScalarAsync()));
    }

    // -------------------------------------------------------------------------
    // F0-27 — Idempotencia en el alta de configuración
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Suscripcion_RechazaOperacionRepetida()
    {
        await using var connection = await OpenAsync();

        await InsertSuscripcionAsync(
            connection,
            SuscripcionId,
            operacionId: "op-suscripcion");

        var exception = await Assert.ThrowsAsync<SqliteException>(
            () => InsertSuscripcionAsync(
                connection,
                "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
                operacionId: "op-suscripcion"));

        Assert.Contains("UNIQUE", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // F0-01 / F0-28 — Cobros sin duplicados y con suscripción existente
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Cobro_RechazaDosCobrosParaLaMismaSuscripcionYFecha()
    {
        await using var connection = await OpenAsync();
        await InsertSuscripcionAsync(connection, SuscripcionId);
        await InsertCobroAsync(connection, CobroId, SuscripcionId, fecha: "2026-10-28");

        var exception = await Assert.ThrowsAsync<SqliteException>(
            () => InsertCobroAsync(
                connection,
                "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
                SuscripcionId,
                fecha: "2026-10-28"));

        Assert.Contains("UNIQUE", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Cobro_RechazaSuscripcionInexistente()
    {
        await using var connection = await OpenAsync();

        var exception = await Assert.ThrowsAsync<SqliteException>(
            () => InsertCobroAsync(connection, CobroId, SuscripcionId));

        Assert.Contains("FOREIGN KEY", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // F0-02 / F0-18 — Un solo pago vigente por cobro
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Pago_RechazaSegundoPagoVigenteParaElMismoCobro()
    {
        await using var connection = await OpenAsync();
        await InsertSuscripcionAsync(connection, SuscripcionId);
        await InsertCobroAsync(connection, CobroId, SuscripcionId);

        await InsertPagoAsync(
            connection,
            "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
            CobroId,
            numeroComprobante: "CMP-2026-000001",
            operacionId: "op-1");

        var exception = await Assert.ThrowsAsync<SqliteException>(
            () => InsertPagoAsync(
                connection,
                "ffffffff-ffff-4fff-8fff-ffffffffffff",
                CobroId,
                numeroComprobante: "CMP-2026-000002",
                operacionId: "op-2"));

        Assert.Contains("UNIQUE", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Pago_PermiteNuevoPagoVigenteTrasAnularElAnterior()
    {
        await using var connection = await OpenAsync();
        await InsertSuscripcionAsync(connection, SuscripcionId);
        await InsertCobroAsync(connection, CobroId, SuscripcionId);

        const string primerPagoId = "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee";

        await InsertPagoAsync(
            connection,
            primerPagoId,
            CobroId,
            numeroComprobante: "CMP-2026-000001",
            operacionId: "op-1");

        await ExecuteAsync(
            connection,
            """
            UPDATE pagos
               SET estado = 'ANULADO',
                   anulado_en = $anuladoEn,
                   motivo_anulacion = 'Fecha de pago incorrecta',
                   actualizado_en = $ts
             WHERE id = $id;
            """,
            ("$anuladoEn", Timestamp),
            ("$ts", Timestamp),
            ("$id", primerPagoId));

        await InsertPagoAsync(
            connection,
            "ffffffff-ffff-4fff-8fff-ffffffffffff",
            CobroId,
            numeroComprobante: "CMP-2026-000002",
            operacionId: "op-2");

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pagos WHERE cobro_id = $cobroId AND estado = 'VIGENTE';";
        command.Parameters.AddWithValue("$cobroId", CobroId);

        Assert.Equal(1L, Convert.ToInt64(await command.ExecuteScalarAsync()));
    }

    // -------------------------------------------------------------------------
    // F0-04 — Pago completo (importe exacto)
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Pago_RechazaImporteDistintoAlCobroProgramado()
    {
        await using var connection = await OpenAsync();
        await InsertSuscripcionAsync(connection, SuscripcionId);
        await InsertCobroAsync(connection, CobroId, SuscripcionId, monto: 1500);

        var exception = await Assert.ThrowsAsync<SqliteException>(
            () => InsertPagoAsync(
                connection,
                "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
                CobroId,
                numeroComprobante: "CMP-2026-000001",
                operacionId: "op-1",
                monto: 1000));

        Assert.Contains("importe del pago", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Pago_PermiteImporteExactoAlCobroProgramado()
    {
        await using var connection = await OpenAsync();
        await InsertSuscripcionAsync(connection, SuscripcionId);
        await InsertCobroAsync(connection, CobroId, SuscripcionId, monto: 1500);

        await InsertPagoAsync(
            connection,
            "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
            CobroId,
            numeroComprobante: "CMP-2026-000001",
            operacionId: "op-1",
            monto: 1500);

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM pagos;";

        Assert.Equal(1L, Convert.ToInt64(await command.ExecuteScalarAsync()));
    }

    // -------------------------------------------------------------------------
    // F0-03 — Comprobante interno
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Pago_RechazaComprobanteDuplicado()
    {
        await using var connection = await OpenAsync();
        await InsertSuscripcionAsync(connection, SuscripcionId);
        await InsertCobroAsync(connection, CobroId, SuscripcionId, fecha: "2026-10-28");
        await InsertCobroAsync(
            connection,
            "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
            SuscripcionId,
            fecha: "2026-11-28");

        await InsertPagoAsync(
            connection,
            "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
            CobroId,
            numeroComprobante: "CMP-2026-000001",
            operacionId: "op-1");

        var exception = await Assert.ThrowsAsync<SqliteException>(
            () => InsertPagoAsync(
                connection,
                "ffffffff-ffff-4fff-8fff-ffffffffffff",
                "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
                numeroComprobante: "CMP-2026-000001",
                operacionId: "op-2"));

        Assert.Contains("UNIQUE", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Pago_RechazaComprobanteConFormatoNoAprobado()
    {
        await using var connection = await OpenAsync();
        await InsertSuscripcionAsync(connection, SuscripcionId);
        await InsertCobroAsync(connection, CobroId, SuscripcionId);

        await Assert.ThrowsAsync<SqliteException>(
            () => InsertPagoAsync(
                connection,
                "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
                CobroId,
                numeroComprobante: "CMP-26-1",
                operacionId: "op-1"));
    }

    // -------------------------------------------------------------------------
    // F0-T02 — Anulación con motivo
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Pago_RechazaAnulacionSinMotivo()
    {
        await using var connection = await OpenAsync();
        await InsertSuscripcionAsync(connection, SuscripcionId);
        await InsertCobroAsync(connection, CobroId, SuscripcionId);

        await Assert.ThrowsAsync<SqliteException>(
            () => InsertPagoAsync(
                connection,
                "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
                CobroId,
                numeroComprobante: "CMP-2026-000001",
                operacionId: "op-1",
                estado: "ANULADO",
                anuladoEn: Timestamp,
                motivoAnulacion: null));
    }

    // -------------------------------------------------------------------------
    // F0-27 — Idempotencia por identificador de operación
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Pago_RechazaOperacionRepetida()
    {
        await using var connection = await OpenAsync();
        await InsertSuscripcionAsync(connection, SuscripcionId);
        await InsertCobroAsync(connection, CobroId, SuscripcionId, fecha: "2026-10-28");
        await InsertCobroAsync(
            connection,
            "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
            SuscripcionId,
            fecha: "2026-11-28");

        await InsertPagoAsync(
            connection,
            "eeeeeeee-eeee-4eee-8eee-eeeeeeeeeeee",
            CobroId,
            numeroComprobante: "CMP-2026-000001",
            operacionId: "op-repetida");

        var exception = await Assert.ThrowsAsync<SqliteException>(
            () => InsertPagoAsync(
                connection,
                "ffffffff-ffff-4fff-8fff-ffffffffffff",
                "dddddddd-dddd-4ddd-8ddd-dddddddddddd",
                numeroComprobante: "CMP-2026-000002",
                operacionId: "op-repetida"));

        Assert.Contains("UNIQUE", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // F0-07 / F0-19 / F0-T08 — Gastos previstos vs confirmados
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Gasto_RechazaEstadoPlanificadoConFechaReal()
    {
        await using var connection = await OpenAsync();

        await Assert.ThrowsAsync<SqliteException>(
            () => InsertGastoAsync(
                connection,
                "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
                estado: "PLANIFICADO",
                fechaReal: "2026-10-28"));
    }

    [Fact]
    public async Task Gasto_RechazaEstadoConfirmadoSinFechaReal()
    {
        await using var connection = await OpenAsync();

        await Assert.ThrowsAsync<SqliteException>(
            () => InsertGastoAsync(
                connection,
                "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
                estado: "CONFIRMADO",
                fechaReal: null));
    }

    [Fact]
    public async Task Gasto_PermitePlanificadoSinFechaRealYConfirmadoConFechaReal()
    {
        await using var connection = await OpenAsync();

        await InsertGastoAsync(
            connection,
            "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
            operacionId: "gasto-op-1",
            estado: "PLANIFICADO",
            fechaReal: null);

        await InsertGastoAsync(
            connection,
            "cccccccc-cccc-4ccc-8ccc-cccccccccccd",
            operacionId: "gasto-op-2",
            estado: "CONFIRMADO",
            fechaReal: "2026-10-28");

        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM gastos;";

        Assert.Equal(2L, Convert.ToInt64(await command.ExecuteScalarAsync()));
    }

    [Fact]
    public async Task Gasto_RechazaCategoriaInexistente()
    {
        await using var connection = await OpenAsync();

        await Assert.ThrowsAsync<SqliteException>(
            () => InsertGastoAsync(
                connection,
                "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
                categoriaId: "99999999-9999-4999-8999-999999999999"));
    }

    // -------------------------------------------------------------------------
    // F0-T07 — Ingresos recurrentes vs extraordinarios
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Ingreso_RechazaExtraordinarioVinculadoAPlantilla()
    {
        await using var connection = await OpenAsync();

        const string plantillaId = "77777777-7777-4777-8777-777777777777";
        await InsertPlantillaAsync(connection, plantillaId);

        var exception = await Assert.ThrowsAsync<SqliteException>(
            () => InsertIngresoAsync(
                connection,
                "88888888-8888-4888-8888-888888888888",
                tipo: "EXTRAORDINARIO",
                plantillaId: plantillaId));

        Assert.Contains("CHECK", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Plantilla_RechazaQuincenalConDiaNoAprobado()
    {
        await using var connection = await OpenAsync();

        await Assert.ThrowsAsync<SqliteException>(
            () => InsertPlantillaAsync(
                connection,
                "77777777-7777-4777-8777-777777777777",
                frecuencia: "QUINCENAL",
                diaAncla: 10));
    }

    [Fact]
    public async Task Plantilla_RechazaOperacionRepetida()
    {
        await using var connection = await OpenAsync();

        await InsertPlantillaAsync(
            connection,
            "77777777-7777-4777-8777-777777777777",
            operacionId: "op-plantilla");

        var exception = await Assert.ThrowsAsync<SqliteException>(
            () => InsertPlantillaAsync(
                connection,
                "99999999-9999-4999-8999-999999999998",
                operacionId: "op-plantilla"));

        Assert.Contains("UNIQUE", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    // -------------------------------------------------------------------------
    // F0-20 / F0-21 / F0-T09 — Saldo inicial único
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SaldoInicial_PermiteUnaSolaFilaVigente()
    {
        await using var connection = await OpenAsync();

        await ExecuteAsync(
            connection,
            """
            INSERT INTO saldo_inicial (id, monto_centavos, fecha_inicio, creado_en, actualizado_en)
            VALUES ('saldo_inicial', 50000, '2026-10-01', $ts, $ts);
            """,
            ("$ts", Timestamp));

        await Assert.ThrowsAsync<SqliteException>(
            () => ExecuteAsync(
                connection,
                """
                INSERT INTO saldo_inicial (id, monto_centavos, fecha_inicio, creado_en, actualizado_en)
                VALUES ('saldo_inicial_2', 10000, '2026-10-01', $ts, $ts);
                """,
                ("$ts", Timestamp)));
    }

    [Fact]
    public async Task SaldoInicial_RechazaImporteNegativo()
    {
        await using var connection = await OpenAsync();

        await Assert.ThrowsAsync<SqliteException>(
            () => ExecuteAsync(
                connection,
                """
                INSERT INTO saldo_inicial (id, monto_centavos, fecha_inicio, creado_en, actualizado_en)
                VALUES ('saldo_inicial', -1, '2026-10-01', $ts, $ts);
                """,
                ("$ts", Timestamp)));
    }

    // -------------------------------------------------------------------------
    // Utilidades
    // -------------------------------------------------------------------------

    private async Task<SqliteConnection> OpenAsync()
    {
        var factory = new SqliteConnectionFactory(_options);
        return await factory.OpenAsync(CancellationToken.None);
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        string sql,
        params (string Name, object? Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value ?? DBNull.Value);
        }

        await command.ExecuteNonQueryAsync();
    }

    private static Task InsertSuscripcionAsync(
        SqliteConnection connection,
        string id,
        long monto = 1500,
        string? proximo = "2026-11-28",
        int activa = 1,
        string? desactivadaEn = null,
        string? operacionId = null)
    {
        return ExecuteAsync(
            connection,
            """
            INSERT INTO suscripciones
                (id, nombre, operacion_id, monto_centavos, tipo_importe, frecuencia,
                 fecha_inicio, proximo_cobro, activa, desactivada_en, creado_en, actualizado_en)
            VALUES
                ($id, 'Netflix', $operacionId, $monto, 'FIJO', 'MENSUAL',
                 '2026-10-28', $proximo, $activa, $desactivadaEn, $ts, $ts);
            """,
            ("$id", id),
            ("$operacionId", operacionId ?? $"op-{id}"),
            ("$monto", monto),
            ("$proximo", proximo),
            ("$activa", activa),
            ("$desactivadaEn", desactivadaEn),
            ("$ts", Timestamp));
    }

    private static Task InsertCobroAsync(
        SqliteConnection connection,
        string id,
        string suscripcionId,
        string fecha = "2026-10-28",
        long monto = 1500,
        string estado = "PENDIENTE")
    {
        return ExecuteAsync(
            connection,
            """
            INSERT INTO cobros_programados
                (id, suscripcion_id, fecha_programada, monto_centavos, estado, creado_en, actualizado_en)
            VALUES
                ($id, $suscripcionId, $fecha, $monto, $estado, $ts, $ts);
            """,
            ("$id", id),
            ("$suscripcionId", suscripcionId),
            ("$fecha", fecha),
            ("$monto", monto),
            ("$estado", estado),
            ("$ts", Timestamp));
    }

    private static Task InsertPagoAsync(
        SqliteConnection connection,
        string id,
        string cobroId,
        string numeroComprobante,
        string operacionId,
        long monto = 1500,
        string fechaPago = "2026-10-27",
        string estado = "VIGENTE",
        string? anuladoEn = null,
        string? motivoAnulacion = null)
    {
        return ExecuteAsync(
            connection,
            """
            INSERT INTO pagos
                (id, cobro_id, operacion_id, fecha_pago, monto_centavos, estado,
                 numero_comprobante, anulado_en, motivo_anulacion, creado_en, actualizado_en)
            VALUES
                ($id, $cobroId, $operacionId, $fechaPago, $monto, $estado,
                 $comprobante, $anuladoEn, $motivo, $ts, $ts);
            """,
            ("$id", id),
            ("$cobroId", cobroId),
            ("$operacionId", operacionId),
            ("$fechaPago", fechaPago),
            ("$monto", monto),
            ("$estado", estado),
            ("$comprobante", numeroComprobante),
            ("$anuladoEn", anuladoEn),
            ("$motivo", motivoAnulacion),
            ("$ts", Timestamp));
    }

    private static Task InsertGastoAsync(
        SqliteConnection connection,
        string id,
        string categoriaId = CategoriaId,
        string operacionId = "gasto-op",
        string estado = "CONFIRMADO",
        string? fechaReal = "2026-10-28",
        string fechaPrevista = "2026-10-28",
        long monto = 3500)
    {
        return ExecuteAsync(
            connection,
            """
            INSERT INTO gastos
                (id, concepto, categoria_id, operacion_id, monto_centavos,
                 fecha_prevista, fecha_real, estado, anulado_en, motivo_anulacion,
                 creado_en, actualizado_en)
            VALUES
                ($id, 'Supermercado', $categoriaId, $operacionId, $monto,
                 $fechaPrevista, $fechaReal, $estado, NULL, NULL, $ts, $ts);
            """,
            ("$id", id),
            ("$categoriaId", categoriaId),
            ("$operacionId", operacionId),
            ("$monto", monto),
            ("$fechaPrevista", fechaPrevista),
            ("$fechaReal", fechaReal),
            ("$estado", estado),
            ("$ts", Timestamp));
    }

    private static Task InsertPlantillaAsync(
        SqliteConnection connection,
        string id,
        string frecuencia = "MENSUAL",
        int diaAncla = 31,
        string? operacionId = null)
    {
        return ExecuteAsync(
            connection,
            """
            INSERT INTO ingresos_plantillas
                (id, concepto, operacion_id, monto_centavos, frecuencia, dia_ancla,
                 fecha_inicio, activa, creado_en, actualizado_en)
            VALUES
                ($id, 'Salario', $operacionId, 60000, $frecuencia, $diaAncla,
                 '2026-10-01', 1, $ts, $ts);
            """,
            ("$id", id),
            ("$operacionId", operacionId ?? $"op-{id}"),
            ("$frecuencia", frecuencia),
            ("$diaAncla", diaAncla),
            ("$ts", Timestamp));
    }

    private static Task InsertIngresoAsync(
        SqliteConnection connection,
        string id,
        string tipo = "MENSUAL",
        string estado = "PREVISTO",
        string? fechaReal = null,
        string? plantillaId = null)
    {
        return ExecuteAsync(
            connection,
            """
            INSERT INTO ingresos
                (id, concepto, plantilla_id, operacion_id, monto_centavos, tipo,
                 fecha_prevista, fecha_real, estado, anulado_en, motivo_anulacion,
                 creado_en, actualizado_en)
            VALUES
                ($id, 'Salario', $plantillaId, $operacionId, 60000, $tipo,
                 '2026-10-30', $fechaReal, $estado, NULL, NULL, $ts, $ts);
            """,
            ("$id", id),
            ("$plantillaId", plantillaId),
            ("$operacionId", $"op-{id}"),
            ("$tipo", tipo),
            ("$fechaReal", fechaReal),
            ("$estado", estado),
            ("$ts", Timestamp));
    }
}
