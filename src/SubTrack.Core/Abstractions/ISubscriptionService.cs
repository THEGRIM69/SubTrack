using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Results;

namespace SubTrack.Core.Abstractions;

/// <summary>
/// F0-T03 — Servicio de suscripciones. Implementado por Infrastructure.
/// </summary>
public interface ISubscriptionService
{
    Task<Result<Subscription>> CreateAsync(
        CreateSubscriptionRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<Subscription>> UpdateAsync(
        UpdateSubscriptionRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<Subscription>> DeactivateAsync(
        DeactivateSubscriptionRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<Subscription>>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Result<Subscription>> GetByIdAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<SubscriptionVersion>>> GetVersionsAsync(
        Guid subscriptionId,
        CancellationToken cancellationToken = default);
}
