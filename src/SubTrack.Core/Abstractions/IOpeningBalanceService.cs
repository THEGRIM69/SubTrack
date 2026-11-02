using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Results;

namespace SubTrack.Core.Abstractions;

/// <summary>
/// F0-20, F0-21, F0-T09 — Servicio del saldo inicial. Implementado por Infrastructure.
/// </summary>
public interface IOpeningBalanceService
{
    Task<Result<OpeningBalance>> GetAsync(
        CancellationToken cancellationToken = default);

    Task<Result<OpeningBalance>> SetAsync(
        SetOpeningBalanceRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<OpeningBalance>> CorrectAsync(
        CorrectOpeningBalanceRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<OpeningBalanceCorrection>>> GetCorrectionsAsync(
        CancellationToken cancellationToken = default);
}
