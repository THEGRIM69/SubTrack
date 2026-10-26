using SubTrack.Core.Domain.Enums;

namespace SubTrack.Core.Domain.Entities;

/// <summary>
/// F0-T06 — Versión de una suscripción con su fecha de entrada en vigor.
/// Los cobros históricos conservan el importe y la frecuencia de su versión.
/// </summary>
public sealed record SubscriptionVersion
{
    public required Guid Id { get; init; }
    public required Guid SubscriptionId { get; init; }
    public required int VersionNumber { get; init; }
    public required DateOnly EffectiveFrom { get; init; }
    public required long AmountCents { get; init; }
    public required AmountType AmountType { get; init; }
    public required BillingFrequency Frequency { get; init; }
    public required int AnchorDay { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
}
