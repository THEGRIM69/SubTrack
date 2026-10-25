namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Resultado global de la ejecución del proceso de migraciones.
/// </summary>
public sealed record MigrationResult(
    bool Success,
    int InitialVersion,
    int FinalVersion,
    IReadOnlyList<MigrationExecutionResult> AppliedMigrations)
{
    /// <summary>Indica si el esquema ya se encontraba al día sin requerir nuevas migraciones.</summary>
    public bool IsUpToDate => AppliedMigrations.Count == 0;
}
