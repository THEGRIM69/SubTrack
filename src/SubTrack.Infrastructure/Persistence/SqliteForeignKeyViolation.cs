namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Violación de clave foránea detectada por <c>PRAGMA foreign_key_check</c>.
/// </summary>
public sealed record SqliteForeignKeyViolation(
    string Table,
    long? RowId,
    string ParentTable,
    int ForeignKeyIndex);
