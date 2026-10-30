using Microsoft.Data.Sqlite;
using SubTrack.Core.Abstractions;
using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Domain.Enums;
using SubTrack.Core.Results;
using SubTrack.Infrastructure.Persistence;
using SubTrack.Infrastructure.Persistence.Repositories;

namespace SubTrack.Infrastructure.Services;

/// <summary>
/// F0-07, F0-12, F0-14, F0-19 — Implementación de gastos independientes.
/// </summary>
internal sealed class ExpenseService : IExpenseService
{
    private readonly SqliteConnectionFactory _factory;
    private readonly SystemClock _clock;
    private readonly ExpenseRepository _expenses = new();
    private readonly ExpenseCategoryRepository _categories = new();

    public ExpenseService(SqliteConnectionFactory factory, SystemClock clock)
    {
        _factory = factory;
        _clock = clock;
    }

    public async Task<Result<Expense>> CreateAsync(
        CreateExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateCreate(request);

        if (errors.Count > 0)
        {
            return Result<Expense>.Fail(errors);
        }

        var now = _clock.UtcNow;
        var plannedDate = request.PlannedDate ?? request.ActualDate ?? _clock.Today;
        var status = request.ActualDate is null
            ? ExpenseStatus.Planned
            : ExpenseStatus.Confirmed;

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            Concept = request.Concept.Trim(),
            CategoryId = request.CategoryId,
            OperationId = request.OperationId,
            AmountCents = request.AmountCents,
            PlannedDate = plannedDate,
            ActualDate = request.ActualDate,
            Status = status,
            CancelledAt = null,
            CancellationReason = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var existing = await _expenses.GetByOperationIdAsync(
                connection,
                transaction,
                request.OperationId,
                cancellationToken).ConfigureAwait(false);

            if (existing is not null)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return Result<Expense>.Ok(existing);
            }

            var category = await _categories.GetByIdAsync(
                connection,
                transaction,
                request.CategoryId,
                cancellationToken).ConfigureAwait(false);

            if (category is null)
            {
                return Result<Expense>.Fail(
                    ErrorCodes.CategoriaNoEncontrada,
                    "La categoría seleccionada no existe.");
            }

            if (!category.IsActive)
            {
                return Result<Expense>.Fail(
                    ErrorCodes.CategoriaInactiva,
                    "La categoría seleccionada está desactivada.");
            }

            await _expenses.InsertAsync(connection, transaction, expense, cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<Expense>.Ok(expense);
        }
        catch (SqliteException exception) when (SqliteErrorTranslator.IsUniqueViolation(exception))
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            var existing = await _expenses.GetByOperationIdAsync(
                connection,
                null,
                request.OperationId,
                cancellationToken).ConfigureAwait(false);

            return existing is not null
                ? Result<Expense>.Ok(existing)
                : Result<Expense>.Fail(
                    ErrorCodes.OperacionYaProcesada,
                    "Esa operación ya fue registrada.");
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<Expense>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<Expense>> UpdateAsync(
        UpdateExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateUpdate(request);

        if (errors.Count > 0)
        {
            return Result<Expense>.Fail(errors);
        }

        var now = _clock.UtcNow;

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var expense = await _expenses.GetByIdAsync(
                connection,
                transaction,
                request.ExpenseId,
                cancellationToken).ConfigureAwait(false);

            if (expense is null)
            {
                return Result<Expense>.Fail(
                    ErrorCodes.GastoNoEncontrado,
                    "El gasto no existe.");
            }

            if (expense.Status == ExpenseStatus.Cancelled)
            {
                return Result<Expense>.Fail(
                    ErrorCodes.GastoYaAnulado,
                    "El gasto está anulado y no admite cambios.");
            }

            var category = await _categories.GetByIdAsync(
                connection,
                transaction,
                request.CategoryId,
                cancellationToken).ConfigureAwait(false);

            if (category is null)
            {
                return Result<Expense>.Fail(
                    ErrorCodes.CategoriaNoEncontrada,
                    "La categoría seleccionada no existe.");
            }

            if (!category.IsActive && category.Id != expense.CategoryId)
            {
                return Result<Expense>.Fail(
                    ErrorCodes.CategoriaInactiva,
                    "La categoría seleccionada está desactivada.");
            }

            var concept = request.Concept.Trim();
            var conceptChanged = !string.Equals(expense.Concept, concept, StringComparison.Ordinal);
            var amountChanged = expense.AmountCents != request.AmountCents;
            var categoryChanged = expense.CategoryId != request.CategoryId;
            var dateChanged = expense.PlannedDate != request.PlannedDate;

            if (!conceptChanged && !amountChanged && !categoryChanged && !dateChanged)
            {
                return Result<Expense>.Ok(expense);
            }

            await _expenses.InsertCorrectionAsync(
                connection,
                transaction,
                new ExpenseCorrection
                {
                    Id = Guid.NewGuid(),
                    ExpenseId = expense.Id,
                    PreviousConcept = conceptChanged ? expense.Concept : null,
                    NewConcept = conceptChanged ? concept : null,
                    PreviousAmountCents = amountChanged ? expense.AmountCents : null,
                    NewAmountCents = amountChanged ? request.AmountCents : null,
                    PreviousCategoryId = categoryChanged ? expense.CategoryId : null,
                    NewCategoryId = categoryChanged ? request.CategoryId : null,
                    PreviousDate = dateChanged ? expense.PlannedDate : null,
                    NewDate = dateChanged ? request.PlannedDate : null,
                    ModifiedAt = now
                },
                cancellationToken).ConfigureAwait(false);

            var updated = expense with
            {
                Concept = concept,
                CategoryId = request.CategoryId,
                AmountCents = request.AmountCents,
                PlannedDate = request.PlannedDate,
                UpdatedAt = now
            };

            await _expenses.UpdateAsync(connection, transaction, updated, cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<Expense>.Ok(updated);
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<Expense>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<Expense>> ConfirmAsync(
        ConfirmExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ActualDate == default)
        {
            return Result<Expense>.Fail(
                new DomainError(
                    ErrorCodes.FechaRequerida,
                    "Debe indicar la fecha real del gasto.",
                    "ActualDate"));
        }

        var now = _clock.UtcNow;

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var expense = await _expenses.GetByIdAsync(
                connection,
                transaction,
                request.ExpenseId,
                cancellationToken).ConfigureAwait(false);

            if (expense is null)
            {
                return Result<Expense>.Fail(
                    ErrorCodes.GastoNoEncontrado,
                    "El gasto no existe.");
            }

            if (expense.Status == ExpenseStatus.Cancelled)
            {
                return Result<Expense>.Fail(
                    ErrorCodes.GastoYaAnulado,
                    "El gasto está anulado y no puede confirmarse.");
            }

            if (expense.Status == ExpenseStatus.Confirmed)
            {
                return expense.ActualDate == request.ActualDate
                    ? Result<Expense>.Ok(expense)
                    : Result<Expense>.Fail(
                        ErrorCodes.GastoYaConfirmado,
                        "El gasto ya estaba confirmado con otra fecha.");
            }

            var confirmed = await _expenses.ConfirmAsync(
                connection,
                transaction,
                expense.Id,
                request.ActualDate,
                now,
                cancellationToken).ConfigureAwait(false);

            if (!confirmed)
            {
                return Result<Expense>.Fail(
                    ErrorCodes.GastoYaConfirmado,
                    "El gasto ya fue confirmado por otra operación.");
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<Expense>.Ok(expense with
            {
                Status = ExpenseStatus.Confirmed,
                ActualDate = request.ActualDate,
                UpdatedAt = now
            });
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<Expense>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<Expense>> CancelAsync(
        CancelExpenseRequest request,
        CancellationToken cancellationToken = default)
    {
        if (DomainValidation.Reason(request.Reason) is { } reasonError)
        {
            return Result<Expense>.Fail(reasonError);
        }

        var now = _clock.UtcNow;

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var expense = await _expenses.GetByIdAsync(
                connection,
                transaction,
                request.ExpenseId,
                cancellationToken).ConfigureAwait(false);

            if (expense is null)
            {
                return Result<Expense>.Fail(
                    ErrorCodes.GastoNoEncontrado,
                    "El gasto no existe.");
            }

            if (expense.Status == ExpenseStatus.Cancelled)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return Result<Expense>.Ok(expense);
            }

            var reason = request.Reason.Trim();

            await _expenses.CancelAsync(
                connection,
                transaction,
                expense.Id,
                now,
                reason,
                cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<Expense>.Ok(expense with
            {
                Status = ExpenseStatus.Cancelled,
                CancelledAt = now,
                CancellationReason = reason,
                UpdatedAt = now
            });
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<Expense>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<Expense>> GetByIdAsync(
        Guid expenseId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var expense = await _expenses.GetByIdAsync(connection, null, expenseId, cancellationToken)
            .ConfigureAwait(false);

        return expense is null
            ? Result<Expense>.Fail(ErrorCodes.GastoNoEncontrado, "El gasto no existe.")
            : Result<Expense>.Ok(expense);
    }

    public async Task<Result<PagedResult<Expense>>> GetHistoryAsync(
        ExpenseHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ServiceSupport.IsValidPage(request.Page))
        {
            return Result<PagedResult<Expense>>.Fail(
                ErrorCodes.RangoExcedido,
                $"El tamaño de página debe estar entre 1 y {PageRequest.MaxPageSize}.");
        }

        if (request.From is { } from && request.To is { } to && to < from)
        {
            return Result<PagedResult<Expense>>.Fail(
                ErrorCodes.RangoExcedido,
                "La fecha final no puede ser anterior a la inicial.");
        }

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var total = await _expenses.CountHistoryAsync(connection, request, cancellationToken)
            .ConfigureAwait(false);

        var items = await _expenses.GetHistoryAsync(connection, request, cancellationToken)
            .ConfigureAwait(false);

        return Result<PagedResult<Expense>>.Ok(
            new PagedResult<Expense>(items, request.Page.Page, request.Page.PageSize, total));
    }

    public async Task<Result<IReadOnlyList<ExpenseCorrection>>> GetCorrectionsAsync(
        Guid expenseId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var expense = await _expenses.GetByIdAsync(connection, null, expenseId, cancellationToken)
            .ConfigureAwait(false);

        if (expense is null)
        {
            return Result<IReadOnlyList<ExpenseCorrection>>.Fail(
                ErrorCodes.GastoNoEncontrado,
                "El gasto no existe.");
        }

        var corrections = await _expenses.GetCorrectionsAsync(connection, expenseId, cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<ExpenseCorrection>>.Ok(corrections);
    }

    private static List<DomainError> ValidateCreate(CreateExpenseRequest request)
    {
        var errors = new List<DomainError>();

        if (DomainValidation.Name(request.Concept) is { } conceptError)
        {
            errors.Add(conceptError);
        }

        if (DomainValidation.Amount(request.AmountCents) is { } amountError)
        {
            errors.Add(amountError);
        }

        if (request.CategoryId == Guid.Empty)
        {
            errors.Add(new DomainError(
                ErrorCodes.CategoriaRequerida,
                "Debe seleccionar una categoría.",
                "CategoryId"));
        }

        if (request.OperationId == Guid.Empty)
        {
            errors.Add(new DomainError(
                ErrorCodes.OperacionInvalida,
                "La operación no es válida. Vuelva a intentarlo.",
                "OperationId"));
        }

        return errors;
    }

    private static List<DomainError> ValidateUpdate(UpdateExpenseRequest request)
    {
        var errors = new List<DomainError>();

        if (DomainValidation.Name(request.Concept) is { } conceptError)
        {
            errors.Add(conceptError);
        }

        if (DomainValidation.Amount(request.AmountCents) is { } amountError)
        {
            errors.Add(amountError);
        }

        if (request.CategoryId == Guid.Empty)
        {
            errors.Add(new DomainError(
                ErrorCodes.CategoriaRequerida,
                "Debe seleccionar una categoría.",
                "CategoryId"));
        }

        if (request.PlannedDate == default)
        {
            errors.Add(new DomainError(
                ErrorCodes.FechaRequerida,
                "Debe indicar la fecha prevista.",
                "PlannedDate"));
        }

        return errors;
    }
}
