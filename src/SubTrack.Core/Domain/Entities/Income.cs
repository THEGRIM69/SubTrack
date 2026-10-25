using SubTrack.Core.Domain.Enums;

namespace SubTrack.Core.Domain.Entities;

/// <summary>
/// F0-06, F0-12, F0-19, F0-T07 — Ingreso mensual, quincenal o extraordinario.
/// Un ingreso previsto no cuenta como recibido hasta confirmarse.
/// </summary>
public sealed record Income
{
    public required Guid Id { get; init; }
    public required string Concept { get; init; }
    public Guid? TemplateId { get; init; }
    public required Guid OperationId { get; init; }
    public required long AmountCents { get; init; }
    public required IncomeType Type { get; init; }
    public required DateOnly PlannedDate { get; init; }
    public DateOnly? ActualDate { get; init; }
    public required IncomeStatus Status { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
