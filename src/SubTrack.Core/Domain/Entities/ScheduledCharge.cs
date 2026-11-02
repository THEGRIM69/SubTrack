using SubTrack.Core.Domain.Enums;

namespace SubTrack.Core.Domain.Entities;

/// <summary>
/// F0-01, F0-05, F0-25 — Obligación programada de una suscripción.
/// Conserva siempre su fecha original, incluso si se paga antes o después.
/// </summary>
public sealed record ScheduledCharge
{
    public required Guid Id { get; init; }
    public required Guid SubscriptionId { get; init; }
    public required DateOnly ScheduledDate { get; init; }
    public required long AmountCents { get; init; }
    public required ScheduledChargeStatus Status { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
