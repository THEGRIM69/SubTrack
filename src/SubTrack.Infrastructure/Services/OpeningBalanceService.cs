using Microsoft.Data.Sqlite;
using SubTrack.Core.Abstractions;
using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Results;
using SubTrack.Infrastructure.Persistence;
using SubTrack.Infrastructure.Persistence.Repositories;

namespace SubTrack.Infrastructure.Services;

/// <summary>
/// F0-20, F0-21, F0-T09 — Implementación del saldo inicial único.
/// </summary>
internal sealed class OpeningBalanceService : IOpeningBalanceService
{
    private readonly SqliteConnectionFactory _factory;
    private readonly SystemClock _clock;
    private readonly OpeningBalanceRepository _balances = new();

    public OpeningBalanceService(SqliteConnectionFactory factory, SystemClock clock)
    {
        _factory = factory;
        _clock = clock;
    }

    public async Task<Result<OpeningBalance>> GetAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var balance = await _balances.GetAsync(connection, null, cancellationToken)
            .ConfigureAwait(false);

        return balance is null
            ? Result<OpeningBalance>.Fail(
                ErrorCodes.SaldoInicialNoConfigurado,
                "Todavía no se ha configurado el saldo inicial.")
            : Result<OpeningBalance>.Ok(balance);
    }

    public async Task<Result<OpeningBalance>> SetAsync(
        SetOpeningBalanceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (Validate(request.AmountCents, request.StartDate) is { } validationError)
        {
            return Result<OpeningBalance>.Fail(validationError);
        }

        var now = _clock.UtcNow;

        var balance = new OpeningBalance
        {
            AmountCents = request.AmountCents,
            StartDate = request.StartDate,
            CreatedAt = now,
            UpdatedAt = now
        };

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var existing = await _balances.GetAsync(connection, transaction, cancellationToken)
                .ConfigureAwait(false);

            if (existing is not null)
            {
                return Result<OpeningBalance>.Fail(
                    ErrorCodes.SaldoInicialYaConfigurado,
                    "El saldo inicial ya está configurado. Use la corrección para modificarlo.");
            }

            await _balances.InsertAsync(connection, transaction, balance, cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<OpeningBalance>.Ok(balance);
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<OpeningBalance>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<OpeningBalance>> CorrectAsync(
        CorrectOpeningBalanceRequest request,
        CancellationToken cancellationToken = default)
    {
        if (Validate(request.AmountCents, request.StartDate) is { } validationError)
        {
            return Result<OpeningBalance>.Fail(validationError);
        }

        var now = _clock.UtcNow;

        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        await using var transaction = connection.BeginTransaction(deferred: false);

        try
        {
            var current = await _balances.GetAsync(connection, transaction, cancellationToken)
                .ConfigureAwait(false);

            if (current is null)
            {
                return Result<OpeningBalance>.Fail(
                    ErrorCodes.SaldoInicialNoConfigurado,
                    "Todavía no se ha configurado el saldo inicial.");
            }

            if (current.AmountCents == request.AmountCents &&
                current.StartDate == request.StartDate)
            {
                return Result<OpeningBalance>.Ok(current);
            }

            await _balances.InsertCorrectionAsync(
                connection,
                transaction,
                new OpeningBalanceCorrection
                {
                    Id = Guid.NewGuid(),
                    PreviousAmountCents = current.AmountCents,
                    NewAmountCents = request.AmountCents,
                    PreviousStartDate = current.StartDate,
                    NewStartDate = request.StartDate,
                    ModifiedAt = now
                },
                cancellationToken).ConfigureAwait(false);

            var updated = current with
            {
                AmountCents = request.AmountCents,
                StartDate = request.StartDate,
                UpdatedAt = now
            };

            await _balances.UpdateAsync(connection, transaction, updated, cancellationToken)
                .ConfigureAwait(false);

            await transaction.CommitAsync(cancellationToken).ConfigureAwait(false);

            return Result<OpeningBalance>.Ok(updated);
        }
        catch (SqliteException exception)
        {
            await ServiceSupport.SafeRollbackAsync(transaction, cancellationToken)
                .ConfigureAwait(false);

            return Result<OpeningBalance>.Fail(SqliteErrorTranslator.Translate(exception));
        }
    }

    public async Task<Result<IReadOnlyList<OpeningBalanceCorrection>>> GetCorrectionsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = await _factory.OpenAsync(cancellationToken)
            .ConfigureAwait(false);

        var corrections = await _balances.GetCorrectionsAsync(connection, cancellationToken)
            .ConfigureAwait(false);

        return Result<IReadOnlyList<OpeningBalanceCorrection>>.Ok(corrections);
    }

    private static DomainError? Validate(long amountCents, DateOnly startDate)
    {
        if (amountCents < 0)
        {
            return new DomainError(
                ErrorCodes.ImporteInvalido,
                "El saldo inicial no puede ser negativo.",
                "AmountCents");
        }

        if (startDate == default)
        {
            return new DomainError(
                ErrorCodes.FechaRequerida,
                "Debe indicar la fecha del saldo inicial.",
                "StartDate");
        }

        return null;
    }
}
