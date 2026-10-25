using System.Globalization;
using Microsoft.Data.Sqlite;

namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Lectura tipada de columnas según las convenciones de almacenamiento:
/// Guid en TEXT, fechas en ISO yyyy-MM-dd y marcas técnicas en ISO UTC.
/// </summary>
internal static class SqliteDataReaderExtensions
{
    public const string DateFormat = "yyyy-MM-dd";

    public static Guid GetGuid(this SqliteDataReader reader, string column) =>
        Guid.Parse(reader.GetString(reader.GetOrdinal(column)));

    public static Guid? GetNullableGuid(this SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : Guid.Parse(reader.GetString(ordinal));
    }

    public static string GetText(this SqliteDataReader reader, string column) =>
        reader.GetString(reader.GetOrdinal(column));

    public static string? GetNullableText(this SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }

    public static long GetCents(this SqliteDataReader reader, string column) =>
        reader.GetInt64(reader.GetOrdinal(column));

    public static long? GetNullableCents(this SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
    }

    public static int GetInt32(this SqliteDataReader reader, string column) =>
        reader.GetInt32(reader.GetOrdinal(column));

    public static bool GetBoolean(this SqliteDataReader reader, string column) =>
        reader.GetInt64(reader.GetOrdinal(column)) == 1;

    public static DateOnly GetDateOnly(this SqliteDataReader reader, string column) =>
        DateOnly.ParseExact(
            reader.GetString(reader.GetOrdinal(column)),
            DateFormat,
            CultureInfo.InvariantCulture);

    public static DateOnly? GetNullableDateOnly(this SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);

        return reader.IsDBNull(ordinal)
            ? null
            : DateOnly.ParseExact(reader.GetString(ordinal), DateFormat, CultureInfo.InvariantCulture);
    }

    public static DateTimeOffset GetTimestamp(this SqliteDataReader reader, string column) =>
        DateTimeOffset.Parse(
            reader.GetString(reader.GetOrdinal(column)),
            CultureInfo.InvariantCulture,
            DateTimeStyles.RoundtripKind);

    public static DateTimeOffset? GetNullableTimestamp(this SqliteDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);

        return reader.IsDBNull(ordinal)
            ? null
            : DateTimeOffset.Parse(
                reader.GetString(ordinal),
                CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind);
    }

    public static string ToStorage(this DateOnly value) =>
        value.ToString(DateFormat, CultureInfo.InvariantCulture);

    public static string ToStorage(this DateTimeOffset value) =>
        value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);
}
