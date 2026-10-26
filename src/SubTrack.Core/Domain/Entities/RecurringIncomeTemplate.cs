using SubTrack.Core.Domain.Enums;

namespace SubTrack.Core.Domain.Entities;

/// <summary>
/// F0-T07 — Plantilla de ingreso recurrente.
/// Cada ingreso previsto se guarda por separado y se vincula a su plantilla.
/// <see cref="AnchorDay"/> vale 15 o 31 (último día del mes) en las quincenales.
/// </summary>
public sealed record RecurringIncomeTemplate
{
    public required Guid Id { get; init; }
    public required string Concept { get; init; }
    public required Guid OperationId { get; init; }
    public required long AmountCents { get; init; }
    public required IncomeType Frequency { get; init; }
    public required int AnchorDay { get; init; }
    public required DateOnly StartDate { get; init; }
    public required bool IsActive { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
