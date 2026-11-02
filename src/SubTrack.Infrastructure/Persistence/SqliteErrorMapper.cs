using Microsoft.Data.Sqlite;

namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Traduce errores de SQLite a errores controlados de SubTrack.
/// No se reintentan errores de restricción; los de bloqueo se informan para
/// que la capa que corresponda aplique una política acotada de reintento.
/// </summary>
internal static class SqliteErrorMapper
{
    public static SubTrackPersistenceException Map(
        SqliteException exception,
        string databasePath)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var kind = exception.SqliteErrorCode switch
        {
            5 => PersistenceFailureKind.Busy,
            6 => PersistenceFailureKind.Locked,
            8 => PersistenceFailureKind.ReadOnly,
            10 => PersistenceFailureKind.IoError,
            11 => PersistenceFailureKind.Corrupt,
            13 => PersistenceFailureKind.DiskFull,
            14 => PersistenceFailureKind.CannotOpen,
            19 => PersistenceFailureKind.Constraint,
            26 => PersistenceFailureKind.NotADatabase,
            _ => PersistenceFailureKind.Unknown
        };

        var message = kind switch
        {
            PersistenceFailureKind.Busy =>
                "La base de datos está ocupada por otra operación. Intente nuevamente.",
            PersistenceFailureKind.Locked =>
                "La base de datos está bloqueada por otra operación. Intente nuevamente.",
            PersistenceFailureKind.ReadOnly =>
                "La base de datos es de solo lectura o no tiene permisos de escritura.",
            PersistenceFailureKind.Constraint =>
                "Una restricción de integridad impidió completar la operación.",
            PersistenceFailureKind.IoError =>
                "Ocurrió un error de entrada/salida al acceder a la base de datos.",
            PersistenceFailureKind.Corrupt =>
                "La base de datos está dañada. Se requiere revisar o restaurar un respaldo.",
            PersistenceFailureKind.DiskFull =>
                "No hay espacio suficiente en disco para completar la operación.",
            PersistenceFailureKind.CannotOpen =>
                "No se pudo abrir la base de datos. Verifique la ruta y los permisos.",
            PersistenceFailureKind.NotADatabase =>
                "El archivo indicado no es una base de datos SQLite válida.",
            _ =>
                "Ocurrió un error de persistencia."
        };

        return new SubTrackPersistenceException(
            message,
            kind,
            databasePath,
            exception.SqliteExtendedErrorCode,
            exception);
    }
}
