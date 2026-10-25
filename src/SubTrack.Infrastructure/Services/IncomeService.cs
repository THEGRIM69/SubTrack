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
/// F0-06, F0-19, F0-T07 — Implementación de ingresos y plantillas recurrentes.
/// </summary>
internal sealed class IncomeService : IIncomeService
{
    private const int MaxHorizonMonths = 12;

    private readonly SqliteConnectionFactory _factory;
    private readonly SystemClock _clock;
    private readonly IncomeRepository _incomes = new();

    public IncomeService(SqliteConnectionFactory factory, SystemClock clock)
    {
        _factory = factory;
        _clock = clock;
    }

    public async Task<Result<RecurringIncomeTemplate>> CreateTemplateAsync(
        CreateIncomeTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateTemplate(request);

        if (errors.Count > 0)
        {
            return Result<RecurringIncomeTemplate>.Fail(errors);
        }

        var now = _clock.UtcNow;

        var template = new RecurringIncomeTemplate
        {
            Id = Guid.NewGuid(),
            Concept = request.Concept.Trim(),
            OperationId = request.OperationId,
            AmountCents = request.AmountCents,
            Frequency = request.Frequency,
            AnchorDay = request.AnchorDay,
            StartDate = request.StartDate,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var existing = await _incomes.GetTemplateByOperationIdAsync(
                connection,
                transaction,
                request.OperationId,
                cancellationToken).ConfigureAwait(false);

            if (existing is not null)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return Result<RecurringIncomeTemplate>.Ok(existing);
            }

            await _incomes.InsertTemplateAsync(connection, transaction, template, cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<RecurringIncomeTemplate>.Ok(template);
        }
        catch (SqliteException exception) when (SqliteErrorTranslator.IsUniqueViolation(exception))
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            var existing = await _incomes.GetTemplateByOperationIdAsync(
                connection,
                null,
                request.OperationId,
                cancellationToken).ConfigureAwait(false);

            return existing is not null
                ? Result<RecurringIncomeTemplate>.Ok(existing)
                : Result<RecurringIncomeTemplate>.Fail(
                    ErrorCodes.OperacionYaProcesada,
                    "Esa operación ya fue registrada.");
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<RecurringIncomeTemplate>.Fail(
                SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<IReadOnlyList<Income>>> GeneratePlannedAsync(
        GeneratePlannedIncomesRequest request,
        CancellationToken cancellationToken = default)
    {
        var today = _clock.Today;

        if (request.Until < today)
        {
            return Result<IReadOnlyList<Income>>.Fail(
                ErrorCodes.RangoExcedido,
                "La fecha límite debe ser hoy o posterior.");
        }

        if (request.Until > today.AddMonths(MaxHorizonMonths))
        {
            return Result<IReadOnlyList<Income>>.Fail(
                ErrorCodes.RangoExcedido,
                $"Solo se pueden generar ingresos previstos con hasta {MaxHorizonMonths} meses de anticipación.");
        }

        var now = _clock.UtcNow;

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var template = await _incomes.GetTemplateAsync(
                connection,
                transaction,
                request.TemplateId,
                cancellationToken).ConfigureAwait(false);

            if (template is null)
            {
                return Result<IReadOnlyList<Income>>.Fail(
                    ErrorCodes.PlantillaNoEncontrada,
                    "La plantilla de ingreso no existe.");
            }

            if (!template.IsActive)
            {
                return Result<IReadOnlyList<Income>>.Fail(
                    ErrorCodes.PlantillaNoEncontrada,
                    "La plantilla de ingreso está desactivada.");
            }

            var from = template.StartDate > today ? template.StartDate : today;
            var created = new List<Income>();

            foreach (var date in PlannedDates(template.Frequency, template.AnchorDay, from, request.Until))
            {
                if (await _incomes.ExistsPlannedAsync(
                    connection,
                    transaction,
                    template.Id,
                    date,
                    cancellationToken).ConfigureAwait(false))
                {
                    continue;
                }

                var income = new Income
                {
                    Id = Guid.NewGuid(),
                    Concept = template.Concept,
                    TemplateId = template.Id,
                    OperationId = Guid.NewGuid(),
                    AmountCents = template.AmountCents,
                    Type = template.Frequency,
                    PlannedDate = date,
                    ActualDate = null,
                    Status = IncomeStatus.Planned,
                    CancelledAt = null,
                    CancellationReason = null,
                    CreatedAt = now,
                    UpdatedAt = now
                };

                await _incomes.InsertAsync(connection, transaction, income, cancellationToken)
                    .ConfigureAwait(false);

                created.Add(income);
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<IReadOnlyList<Income>>.Ok(created);
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<IReadOnlyList<Income>>.Fail(
                SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<Income>> CreateAsync(
        CreateIncomeRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateCreate(request);

        if (errors.Count > 0)
        {
            return Result<Income>.Fail(errors);
        }

        var now = _clock.UtcNow;
        var plannedDate = request.PlannedDate ?? request.ActualDate ?? _clock.Today;
        var status = request.ActualDate is null ? IncomeStatus.Planned : IncomeStatus.Confirmed;

        var income = new Income
        {
            Id = Guid.NewGuid(),
            Concept = request.Concept.Trim(),
            TemplateId = request.TemplateId,
            OperationId = request.OperationId,
            AmountCents = request.AmountCents,
            Type = request.Type,
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
            var existing = await _incomes.GetByOperationIdAsync(
                connection,
                transaction,
                request.OperationId,
                cancellationToken).ConfigureAwait(false);

            if (existing is not null)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return Result<Income>.Ok(existing);
            }

            if (request.TemplateId is { } templateId)
            {
                var template = await _incomes.GetTemplateAsync(
                    connection,
                    transaction,
                    templateId,
                    cancellationToken).ConfigureAwait(false);

                if (template is null)
                {
                    return Result<Income>.Fail(
                        ErrorCodes.PlantillaNoEncontrada,
                        "La plantilla de ingreso no existe.");
                }
            }

            await _incomes.InsertAsync(connection, transaction, income, cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<Income>.Ok(income);
        }
        catch (SqliteException exception) when (SqliteErrorTranslator.IsUniqueViolation(exception))
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            var existing = await _incomes.GetByOperationIdAsync(
                connection,
                null,
                request.OperationId,
                cancellationToken).ConfigureAwait(false);

            return existing is not null
                ? Result<Income>.Ok(existing)
                : Result<Income>.Fail(
                    ErrorCodes.OperacionYaProcesada,
                    "Esa operación ya fue registrada.");
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<Income>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<Income>> UpdateAsync(
        UpdateIncomeRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateUpdate(request);

        if (errors.Count > 0)
        {
            return Result<Income>.Fail(errors);
        }

        var now = _clock.UtcNow;

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var income = await _incomes.GetByIdAsync(
                connection,
                transaction,
                request.IncomeId,
                cancellationToken).ConfigureAwait(false);

            if (income is null)
            {
                return Result<Income>.Fail(
                    ErrorCodes.IngresoNoEncontrado,
                    "El ingreso no existe.");
            }

            if (income.Status == IncomeStatus.Cancelled)
            {
                return Result<Income>.Fail(
                    ErrorCodes.IngresoYaAnulado,
                    "El ingreso está anulado y no admite cambios.");
            }

            var concept = request.Concept.Trim();
            var conceptChanged = !string.Equals(income.Concept, concept, StringComparison.Ordinal);
            var amountChanged = income.AmountCents != request.AmountCents;
            var dateChanged = income.PlannedDate != request.PlannedDate;

            if (!conceptChanged && !amountChanged && !dateChanged)
            {
                return Result<Income>.Ok(income);
            }

            await _incomes.InsertCorrectionAsync(
                connection,
                transaction,
                new IncomeCorrection
                {
                    Id = Guid.NewGuid(),
                    IncomeId = income.Id,
                    PreviousConcept = conceptChanged ? income.Concept : null,
                    NewConcept = conceptChanged ? concept : null,
                    PreviousAmountCents = amountChanged ? income.AmountCents : null,
                    NewAmountCents = amountChanged ? request.AmountCents : null,
                    PreviousDate = dateChanged ? income.PlannedDate : null,
                    NewDate = dateChanged ? request.PlannedDate : null,
                    ModifiedAt = now
                },
                cancellationToken).ConfigureAwait(false);

            var updated = income with
            {
                Concept = concept,
                AmountCents = request.AmountCents,
                PlannedDate = request.PlannedDate,
                UpdatedAt = now
            };

            await _incomes.UpdateAsync(connection, transaction, updated, cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<Income>.Ok(updated);
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<Income>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<Income>> ConfirmAsync(
        ConfirmIncomeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ActualDate == default)
        {
            return Result<Income>.Fail(
                new DomainError(
                    ErrorCodes.FechaRequerida,
                    "Debe indicar la fecha real del ingreso.",
                    "ActualDate"));
        }

        var now = _clock.UtcNow;

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var income = await _incomes.GetByIdAsync(
                connection,
                transaction,
                request.IncomeId,
                cancellationToken).ConfigureAwait(false);

            if (income is null)
            {
                return Result<Income>.Fail(
                    ErrorCodes.IngresoNoEncontrado,
                    "El ingreso no existe.");
            }

            if (income.Status == IncomeStatus.Cancelled)
            {
                return Result<Income>.Fail(
                    ErrorCodes.IngresoYaAnulado,
                    "El ingreso está anulado y no puede confirmarse.");
            }

            if (income.Status == IncomeStatus.Confirmed)
            {
                return income.ActualDate == request.ActualDate
                    ? Result<Income>.Ok(income)
                    : Result<Income>.Fail(
                        ErrorCodes.IngresoYaConfirmado,
                        "El ingreso ya estaba confirmado con otra fecha.");
            }

            var confirmed = await _incomes.ConfirmAsync(
                connection,
                transaction,
                income.Id,
                request.ActualDate,
                now,
                cancellationToken).ConfigureAwait(false);

            if (!confirmed)
            {
                return Result<Income>.Fail(
                    ErrorCodes.IngresoYaConfirmado,
                    "El ingreso ya fue confirmado por otra operación.");
            }

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<Income>.Ok(income with
            {
                Status = IncomeStatus.Confirmed,
                ActualDate = request.ActualDate,
                UpdatedAt = now
            });
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<Income>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<Income>> CancelAsync(
        CancelIncomeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (DomainValidation.Reason(request.Reason) is { } reasonError)
        {
            return Result<Income>.Fail(reasonError);
        }

        var now = _clock.UtcNow;

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var income = await _incomes.GetByIdAsync(
                connection,
                transaction,
                request.IncomeId,
                cancellationToken).ConfigureAwait(false);

            if (income is null)
            {
                return Result<Income>.Fail(
                    ErrorCodes.IngresoNoEncontrado,
                    "El ingreso no existe.");
            }

            if (income.Status == IncomeStatus.Cancelled)
            {
                await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
                return Result<Income>.Ok(income);
            }

            var reason = request.Reason.Trim();

            await _incomes.CancelAsync(
                connection,
                transaction,
                income.Id,
                now,
                reason,
                cancellationToken).ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<Income>.Ok(income with
            {
                Status = IncomeStatus.Cancelled,
                CancelledAt = now,
                CancellationReason = reason,
                UpdatedAt = now
            });
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<Income>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<Income>> GetByIdAsync(
        Guid incomeId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var income = await _incomes.GetByIdAsync(connection, null, incomeId, cancellationToken)
            .ConfigureAwait(false);

        return income is null
            ? Result<Income>.Fail(ErrorCodes.IngresoNoEncontrado, "El ingreso no existe.")
            : Result<Income>.Ok(income);
    }

    public async Task<Result<PagedResult<Income>>> GetHistoryAsync(
        IncomeHistoryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ServiceSupport.IsValidPage(request.Page))
        {
            return Result<PagedResult<Income>>.Fail(
                ErrorCodes.RangoExcedido,
                $"El tamaño de página debe estar entre 1 y {PageRequest.MaxPageSize}.");
        }

        if (request.From is { } from && request.To is { } to && to < from)
        {
            return Result<PagedResult<Income>>.Fail(
                ErrorCodes.RangoExcedido,
                "La fecha final no puede ser anterior a la inicial.");
        }

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var total = await _incomes.CountHistoryAsync(connection, request, cancellationToken)
            .ConfigureAwait(false);

        var items = await _incomes.GetHistoryAsync(connection, request, cancellationToken)
            .ConfigureAwait(false);

        return Result<PagedResult<Income>>.Ok(
            new PagedResult<Income>(items, request.Page.Page, request.Page.PageSize, total));
    }

    public async Task<Result<IReadOnlyList<IncomeCorrection>>> GetCorrectionsAsync(
        Guid incomeId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var income = await _incomes.GetByIdAsync(connection, null, incomeId, cancellationToken)
            .ConfigureAwait(false);

        if (income is null)
        {
            return Result<IReadOnlyList<IncomeCorrection>>.Fail(
                ErrorCodes.IngresoNoEncontrado,
                "El ingreso no existe.");
        }

        var corrections = await _incomes.GetCorrectionsAsync(connection, incomeId, cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<IncomeCorrection>>.Ok(corrections);
    }

    /// <summary>
    /// F0-T07 — Fechas previstas. Mensual respeta el día ancla con ajuste al
    /// último día del mes; quincenal genera los días 15 y último de cada mes.
    /// </summary>
    private static IReadOnlyList<DateOnly> PlannedDates(
        IncomeType type,
        int anchorDay,
        DateOnly from,
        DateOnly until)
    {
        if (from > until)
        {
            return Array.Empty<DateOnly>();
        }

        var dates = new List<DateOnly>();
        var firstOfMonth = new DateOnly(from.Year, from.Month, 1);

        for (var month = firstOfMonth; month <= until; month = month.AddMonths(1))
        {
            var lastDay = DateTime.DaysInMonth(month.Year, month.Month);

            if (type == IncomeType.Biweekly)
            {
                var fifteenth = new DateOnly(month.Year, month.Month, 15);

                if (fifteenth >= from && fifteenth <= until)
                {
                    dates.Add(fifteenth);
                }
            }

            var day = type == IncomeType.Biweekly
                ? lastDay
                : Math.Min(anchorDay, lastDay);

            var date = new DateOnly(month.Year, month.Month, day);

            if (date >= from && date <= until)
            {
                dates.Add(date);
            }
        }

        return dates;
    }

    private static List<DomainError> ValidateTemplate(CreateIncomeTemplateRequest request)
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

        if (request.Frequency is not (IncomeType.Monthly or IncomeType.Biweekly))
        {
            errors.Add(new DomainError(
                ErrorCodes.EstadoInicialInvalido,
                "Un ingreso recurrente solo puede ser mensual o quincenal.",
                "Frequency"));
        }

        var anchorError = request.Frequency == IncomeType.Biweekly
            ? DomainValidation.BiweeklyAnchorDay(request.AnchorDay)
            : DomainValidation.AnchorDay(request.AnchorDay);

        if (anchorError is not null)
        {
            errors.Add(anchorError);
        }

        if (request.StartDate == default)
        {
            errors.Add(new DomainError(
                ErrorCodes.FechaRequerida,
                "Debe indicar la fecha de inicio.",
                "StartDate"));
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

    private static List<DomainError> ValidateCreate(CreateIncomeRequest request)
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

        if (!Enum.IsDefined(request.Type))
        {
            errors.Add(new DomainError(
                ErrorCodes.EstadoInicialInvalido,
                "El tipo de ingreso no es válido.",
                "Type"));
        }

        if (request.Type == IncomeType.Extraordinary)
        {
            if (request.TemplateId is not null)
            {
                errors.Add(new DomainError(
                    ErrorCodes.EstadoInicialInvalido,
                    "Un ingreso extraordinario no puede provenir de una plantilla.",
                    "TemplateId"));
            }

            if (request.ActualDate is null)
            {
                errors.Add(new DomainError(
                    ErrorCodes.FechaRequerida,
                    "Un ingreso extraordinario requiere la fecha real en que se recibió.",
                    "ActualDate"));
            }
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

    private static List<DomainError> ValidateUpdate(UpdateIncomeRequest request)
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
