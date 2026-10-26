using Microsoft.Data.Sqlite;
using SubTrack.Core.Results;

namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Traduce errores técnicos de SQLite a errores de dominio.
/// Ningún mensaje crudo de SQLite llega a la interfaz (F0-15).
/// </summary>
internal static class SqliteErrorTranslator
{
    private const int UniqueConstraint = 2067;
    private const int PrimaryKeyConstraint = 1555;
    private const int ForeignKeyConstraint = 787;
    private const int CheckConstraint = 275;
    private const int NotNullConstraint = 1299;

    /// <summary>
    /// Indica si el error es una violación de unicidad, útil para resolver
    /// reintentos idempotentes que perdieron la carrera contra otra conexión.
    /// </summary>
    public static bool IsUniqueViolation(SqliteException exception) =>
        exception.SqliteExtendedErrorCode == UniqueConstraint;

    public static DomainError Translate(SqliteException exception, string? uniqueConflictCode = null)
    {
        return exception.SqliteExtendedErrorCode switch
        {
            UniqueConstraint => new DomainError(
                uniqueConflictCode ?? ErrorCodes.ErrorInterno,
                "Ya existe un registro con esos datos."),

            PrimaryKeyConstraint => new DomainError(
                ErrorCodes.ErrorInterno,
                "El registro ya existe con ese identificador."),

            ForeignKeyConstraint => new DomainError(
                ErrorCodes.ErrorInterno,
                "La operación hace referencia a datos que no existen."),

            CheckConstraint => new DomainError(
                ErrorCodes.ErrorInterno,
                "Los datos no cumplen una regla de integridad."),

            NotNullConstraint => new DomainError(
                ErrorCodes.ErrorInterno,
                "Falta un dato obligatorio."),

            5 or 6 => new DomainError(
                ErrorCodes.BaseOcupada,
                "La base de datos está ocupada. Espere un momento e intente de nuevo."),

            11 or 26 => new DomainError(
                ErrorCodes.BaseCorrupta,
                "La base de datos presenta un problema de integridad. Use un respaldo válido."),

            8 or 10 or 13 or 14 => new DomainError(
                ErrorCodes.BaseNoDisponible,
                "No se pudo acceder a la base de datos. Compruebe la ruta y el espacio en disco."),

            _ => new DomainError(
                ErrorCodes.ErrorInterno,
                "Ocurrió un error inesperado al acceder a los datos.")
        };
    }
}
