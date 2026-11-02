using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Results;

namespace SubTrack.Core.Abstractions;

/// <summary>
/// F0-T03 — Servicio de gastos independientes. Implementado por Infrastructure.
/// </summary>
public interface IExpenseService
{
    Task<Result<Expense>> CreateAsync(
        CreateExpenseRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<Expense>> UpdateAsync(
        UpdateExpenseRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<Expense>> ConfirmAsync(
        ConfirmExpenseRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<Expense>> CancelAsync(
        CancelExpenseRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<Expense>> GetByIdAsync(
        Guid expenseId,
        CancellationToken cancellationToken = default);

    Task<Result<PagedResult<Expense>>> GetHistoryAsync(
        ExpenseHistoryRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ExpenseCorrection>>> GetCorrectionsAsync(
        Guid expenseId,
        CancellationToken cancellationToken = default);
}
