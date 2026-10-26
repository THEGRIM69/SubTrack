using Microsoft.Data.Sqlite;
using SubTrack.Core.Abstractions;
using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Results;
using SubTrack.Infrastructure.Persistence;
using SubTrack.Infrastructure.Persistence.Repositories;

namespace SubTrack.Infrastructure.Services;

/// <summary>
/// F0-13, F0-T11 — Implementación de categorías de gasto.
/// </summary>
internal sealed class ExpenseCategoryService : IExpenseCategoryService
{
    private readonly SqliteConnectionFactory _factory;
    private readonly SystemClock _clock;
    private readonly ExpenseCategoryRepository _categories = new();

    public ExpenseCategoryService(SqliteConnectionFactory factory, SystemClock clock)
    {
        _factory = factory;
        _clock = clock;
    }

    public async Task<Result<ExpenseCategory>> CreateAsync(
        CreateExpenseCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (ValidateName(request.Name) is { } nameError)
        {
            return Result<ExpenseCategory>.Fail(nameError);
        }

        var now = _clock.UtcNow;

        var category = new ExpenseCategory
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var duplicate = await _categories.GetByNameAsync(
                connection,
                transaction,
                category.Name,
                null,
                cancellationToken).ConfigureAwait(false);

            if (duplicate is not null)
            {
                return Result<ExpenseCategory>.Fail(
                    ErrorCodes.CategoriaDuplicada,
                    "Ya existe una categoría con ese nombre.");
            }

            await _categories.InsertAsync(connection, transaction, category, cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<ExpenseCategory>.Ok(category);
        }
        catch (SqliteException exception) when (SqliteErrorTranslator.IsUniqueViolation(exception))
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<ExpenseCategory>.Fail(
                ErrorCodes.CategoriaDuplicada,
                "Ya existe una categoría con ese nombre.");
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<ExpenseCategory>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<ExpenseCategory>> UpdateAsync(
        UpdateExpenseCategoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (ValidateName(request.Name) is { } nameError)
        {
            return Result<ExpenseCategory>.Fail(nameError);
        }

        var now = _clock.UtcNow;

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var category = await _categories.GetByIdAsync(
                connection,
                transaction,
                request.CategoryId,
                cancellationToken).ConfigureAwait(false);

            if (category is null)
            {
                return Result<ExpenseCategory>.Fail(
                    ErrorCodes.CategoriaNoEncontrada,
                    "La categoría no existe.");
            }

            if (string.Equals(category.Name, request.Name.Trim(), StringComparison.Ordinal))
            {
                return Result<ExpenseCategory>.Ok(category);
            }

            var duplicate = await _categories.GetByNameAsync(
                connection,
                transaction,
                request.Name,
                request.CategoryId,
                cancellationToken).ConfigureAwait(false);

            if (duplicate is not null)
            {
                return Result<ExpenseCategory>.Fail(
                    ErrorCodes.CategoriaDuplicada,
                    "Ya existe otra categoría con ese nombre.");
            }

            await _categories.RenameAsync(
                connection,
                transaction,
                category.Id,
                request.Name,
                now,
                cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<ExpenseCategory>.Ok(category with
            {
                Name = request.Name.Trim(),
                UpdatedAt = now
            });
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<ExpenseCategory>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<ExpenseCategory>> DeactivateAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        var now = _clock.UtcNow;

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var category = await _categories.GetByIdAsync(
                connection,
                transaction,
                categoryId,
                cancellationToken).ConfigureAwait(false);

            if (category is null)
            {
                return Result<ExpenseCategory>.Fail(
                    ErrorCodes.CategoriaNoEncontrada,
                    "La categoría no existe.");
            }

            if (!category.IsActive)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return Result<ExpenseCategory>.Ok(category);
            }

            await _categories.SetActiveAsync(
                connection,
                transaction,
                category.Id,
                false,
                now,
                cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<ExpenseCategory>.Ok(category with
            {
                IsActive = false,
                UpdatedAt = now
            });
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<ExpenseCategory>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result> DeleteAsync(
        Guid categoryId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var category = await _categories.GetByIdAsync(
                connection,
                transaction,
                categoryId,
                cancellationToken).ConfigureAwait(false);

            if (category is null)
            {
                return Result.Fail(
                    ErrorCodes.CategoriaNoEncontrada,
                    "La categoría no existe.");
            }

            var hasExpenses = await _categories.HasExpensesAsync(
                connection,
                transaction,
                categoryId,
                cancellationToken).ConfigureAwait(false);

            if (hasExpenses)
            {
                return Result.Fail(
                    ErrorCodes.CategoriaConGastos,
                    "La categoría tiene gastos asociados. Desactívela en lugar de eliminarla.");
            }

            await _categories.DeleteAsync(connection, transaction, categoryId, cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result.Ok();
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<IReadOnlyList<ExpenseCategory>>> GetAllAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var categories = await _categories.GetAllAsync(connection, includeInactive, cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<ExpenseCategory>>.Ok(categories);
    }

    private static DomainError? ValidateName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new DomainError(
                ErrorCodes.NombreRequerido,
                "Debe escribir un nombre para la categoría.",
                "Name");
        }

        return name.Trim().Length > DomainValidation.MaxCategoryNameLength
            ? new DomainError(
                ErrorCodes.NombreDemasiadoLargo,
                $"El nombre no puede superar los {DomainValidation.MaxCategoryNameLength} caracteres.",
                "Name")
            : null;
    }
}
