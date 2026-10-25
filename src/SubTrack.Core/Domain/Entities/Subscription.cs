using SubTrack.Core.Domain.Enums;

namespace SubTrack.Core.Domain.Entities;

/// <summary>
/// Suscripción registrada por el usuario.
/// <see cref="AnchorDay"/> se obtiene de la versión vigente y
/// <see cref="NextChargeDate"/> es null solo si está inactiva y no quedan cobros pendientes.
/// </summary>
public sealed record Subscription
{
    public required Guid Id { get; init; }
    public required string Name { get; init; }
    public required Guid OperationId { get; init; }
    public required long AmountCents { get; init; }
    public required AmountType AmountType { get; init; }
    public required BillingFrequency Frequency { get; init; }
    public required DateOnly StartDate { get; init; }
    public required int AnchorDay { get; init; }
    public DateOnly? NextChargeDate { get; init; }
    public required bool IsActive { get; init; }
    public DateOnly? DeactivatedOn { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
