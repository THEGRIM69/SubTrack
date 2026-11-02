using System.Security.Cryptography;
using System.Text;

namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Representa una migración de base de datos SQLite versionada.
/// </summary>
public sealed record SqliteMigration
{
    public SqliteMigration(int version, string name, string sqlContent)
    {
        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(version),
                "La versión de la migración debe ser un entero positivo mayor que cero.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "El nombre de la migración no puede estar vacío.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(sqlContent))
        {
            throw new ArgumentException(
                "El contenido SQL de la migración no puede estar vacío.",
                nameof(sqlContent));
        }

        Version = version;
        Name = name.Trim();
        SqlContent = sqlContent;
    }

    /// <summary>Número de versión secuencial (1, 2, 3...).</summary>
    public int Version { get; }

    /// <summary>Nombre descriptivo de la migración.</summary>
    public string Name { get; }

    /// <summary>Instrucciones SQL DDL/DML que componen la migración.</summary>
    public string SqlContent { get; }

    /// <summary>
    /// Calcula el checksum SHA-256 del script SQL normalizado (LF y sin espacios sobrantes),
    /// asegurando resultados idénticos con independencia de los saltos de línea del sistema operativo.
    /// </summary>
    public string CalculateChecksum()
    {
        var normalized = SqlContent
            .Replace("\r\n", "\n")
            .Replace("\r", "\n")
            .Trim();
        var bytes = Encoding.UTF8.GetBytes(normalized);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
