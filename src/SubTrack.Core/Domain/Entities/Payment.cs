using SubTrack.Core.Domain.Enums;

namespace SubTrack.Core.Domain.Entities;

/// <summary>
/// F0-02, F0-03, F0-04 — Pago de un cobro programado.
/// El importe coincide siempre con el cobro y el comprobante es único.
/// Un pago anulado conserva su número de comprobante original.
/// </summary>
public sealed record Payment
{
    public required Guid Id { get; init; }
    public required Guid ScheduledChargeId { get; init; }
    public required Guid OperationId { get; init; }
    public required DateOnly PaymentDate { get; init; }
    public required long AmountCents { get; init; }
    public required PaymentStatus Status { get; init; }
    public required string VoucherNumber { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public string? CancellationReason { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }
}
