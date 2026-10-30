using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Results;

namespace SubTrack.Core.Abstractions;

/// <summary>
/// F0-T03 — Servicio de pagos. Implementado por Infrastructure.
/// </summary>
public interface IPaymentService
{
    Task<Result<Payment>> RegisterAsync(
        RegisterPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<Payment>> CancelAsync(
        CancelPaymentRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<Payment>> GetByIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<Payment>>> GetByScheduledChargeIdAsync(
        Guid scheduledChargeId,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<Payment>>> GetHistoryAsync(
        PageRequest page,
        CancellationToken cancellationToken = default);
}
