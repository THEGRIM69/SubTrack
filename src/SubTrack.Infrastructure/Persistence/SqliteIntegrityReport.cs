namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Resultado de la verificación de integridad estructural y de claves foráneas.
/// </summary>
public sealed record SqliteIntegrityReport(
    string DatabasePath,
    IReadOnlyList<string> IntegrityMessages,
    IReadOnlyList<SqliteForeignKeyViolation> ForeignKeyViolations,
    SqliteDatabaseDiagnostics Diagnostics,
    DateTimeOffset CheckedAtUtc)
{
    /// <summary>Indica si <c>PRAGMA integrity_check</c> devolvió únicamente <c>ok</c>.</summary>
    public bool IntegrityOk =>
        IntegrityMessages.Count == 1 &&
        string.Equals(IntegrityMessages[0], "ok", StringComparison.OrdinalIgnoreCase);

    /// <summary>Indica si <c>PRAGMA foreign_key_check</c> no encontró violaciones.</summary>
    public bool ForeignKeysOk => ForeignKeyViolations.Count == 0;

    /// <summary>Resultado global: integridad estructural y claves foráneas correctas.</summary>
    public bool IsHealthy => IntegrityOk && ForeignKeysOk;
}
