using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Results;

namespace SubTrack.Core.Abstractions;

/// <summary>
/// F0-T03 — Servicio de cobros programados. Implementado por Infrastructure.
/// </summary>
public interface IScheduledChargeService
{
    Task<Result<IReadOnlyList<ScheduledCharge>>> GenerateAsync(
        GenerateChargesRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ScheduledCharge>>> GetByRangeAsync(
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ScheduledCharge>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ScheduledCharge>>> GetPendingAsync(
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ScheduledCharge>>> GetOverdueAsync(
        DateOnly asOf,
        CancellationToken cancellationToken = default);

    Task<Result<ScheduledCharge>> AdjustAmountAsync(
        AdjustChargeAmountRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ScheduledCharge>> RegisterInitialPendingAsync(
        RegisterInitialPendingChargeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ScheduledCharge>> CancelAsync(
        Guid scheduledChargeId,
        CancellationToken cancellationToken = default);
}
