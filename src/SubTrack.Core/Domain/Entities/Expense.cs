using SubTrack.Core.Domain.Enums;

namespace SubTrack.Core.Domain.Entities;

/// <summary>
/// F0-07, F0-12, F0-19, F0-T08 — Gasto independiente.
/// Conserva la fecha prevista y la fecha real por separado; el Dashboard
/// usa <see cref="ActualDate"/> únicamente cuando el gasto está confirmado.
/// </summary>
public sealed record Expense
{
    public required Guid Id { get; init; }
    public required string Concept { get; init; }
    public required Guid CategoryId { get; init; }
    public required Guid OperationId { get; init; }
    public required long AmountCents { get; init; }
    public required DateOnly PlannedDate { get; init; }
    public DateOnly? ActualDate { get; init; }
    public required ExpenseStatus Status { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
