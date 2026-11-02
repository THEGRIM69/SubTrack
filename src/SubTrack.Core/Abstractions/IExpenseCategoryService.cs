using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Results;

namespace SubTrack.Core.Abstractions;

/// <summary>
/// F0-13, F0-T11 — Servicio de categorías de gasto. Implementado por Infrastructure.
/// </summary>
public interface IExpenseCategoryService
{
    Task<Result<ExpenseCategory>> CreateAsync(
        CreateExpenseCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ExpenseCategory>> UpdateAsync(
        UpdateExpenseCategoryRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ExpenseCategory>> DeactivateAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<ExpenseCategory>>> GetAllAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default);
}
