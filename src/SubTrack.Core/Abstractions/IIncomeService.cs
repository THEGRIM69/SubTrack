using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Results;

namespace SubTrack.Core.Abstractions;

/// <summary>
/// F0-T03 — Servicio de ingresos. Implementado por Infrastructure.
/// </summary>
public interface IIncomeService
{
    Task<Result<RecurringIncomeTemplate>> CreateTemplateAsync(
        CreateIncomeTemplateRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<Income>>> GeneratePlannedAsync(
        GeneratePlannedIncomesRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<Income>> CreateAsync(
        CreateIncomeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<Income>> UpdateAsync(
        UpdateIncomeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<Income>> ConfirmAsync(
        ConfirmIncomeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<Income>> CancelAsync(
        CancelIncomeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<Income>> GetByIdAsync(
        Guid incomeId,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<Income>>> GetHistoryAsync(
        IncomeHistoryRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<IncomeCorrection>>> GetCorrectionsAsync(
        Guid incomeId,
        CancellationToken cancellationToken = default);
}
