using Microsoft.Data.Sqlite;
using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Core.Domain.Enums;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Infrastructure.Persistence.Repositories;

/// <summary>
/// Acceso a gastos independientes y a su historial de correcciones (F0-14).
/// </summary>
internal sealed class ExpenseRepository
{
    private const string SelectExpenses = """
        SELECT id, concepto, categoria_id, operacion_id, monto_centavos,
               fecha_prevista, fecha_real, estado, anulado_en, motivo_anulacion,
               creado_en, actualizado_en
        FROM gastos
        """;

    private const string EffectiveDate = "COALESCE(fecha_real, fecha_prevista)";

    public async Task<Expense?> GetByIdAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid expenseId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(transaction, $"{SelectExpenses} WHERE id = $id;")
            .WithParameters(("$id", expenseId.ToString()));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? Map(reader)
            : null;
    }

    public async Task<Expense?> GetByOperationIdAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(transaction, $"{SelectExpenses} WHERE operacion_id = $operacion;")
            .WithParameters(("$operacion", operationId.ToString()));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? Map(reader)
            : null;
    }

    public async Task InsertAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Expense expense,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                INSERT INTO gastos
                    (id, concepto, categoria_id, operacion_id, monto_centavos,
                     fecha_prevista, fecha_real, estado, anulado_en, motivo_anulacion,
                     creado_en, actualizado_en)
                VALUES
                    ($id, $concepto, $categoria, $operacion, $monto,
                     $prevista, $real, $estado, $anulado, $motivo, $creado, $actualizado);
                """)
            .WithParameters(
                ("$id", expense.Id.ToString()),
                ("$concepto", expense.Concept),
                ("$categoria", expense.CategoryId.ToString()),
                ("$operacion", expense.OperationId.ToString()),
                ("$monto", expense.AmountCents),
                ("$prevista", expense.PlannedDate.ToStorage()),
                ("$real", expense.ActualDate?.ToStorage()),
                ("$estado", SqliteEnumCodes.ToCode(expense.Status)),
                ("$anulado", expense.CancelledAt?.ToStorage()),
                ("$motivo", expense.CancellationReason),
                ("$creado", expense.CreatedAt.ToStorage()),
                ("$actualizado", expense.UpdatedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Expense expense,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                UPDATE gastos
                SET concepto = $concepto,
                    categoria_id = $categoria,
                    monto_centavos = $monto,
                    fecha_prevista = $prevista,
                    actualizado_en = $actualizado
                WHERE id = $id;
                """)
            .WithParameters(
                ("$id", expense.Id.ToString()),
                ("$concepto", expense.Concept),
                ("$categoria", expense.CategoryId.ToString()),
                ("$monto", expense.AmountCents),
                ("$prevista", expense.PlannedDate.ToStorage()),
                ("$actualizado", expense.UpdatedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ConfirmAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid expenseId,
        DateOnly actualDate,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                UPDATE gastos
                SET estado = 'CONFIRMADO',
                    fecha_real = $real,
                    actualizado_en = $actualizado
                WHERE id = $id AND estado = 'PLANIFICADO';
                """)
            .WithParameters(
                ("$id", expenseId.ToString()),
                ("$real", actualDate.ToStorage()),
                ("$actualizado", updatedAt.ToStorage()));

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
    }

    public async Task<bool> CancelAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid expenseId,
        DateTimeOffset cancelledAt,
        string reason,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                UPDATE gastos
                SET estado = 'ANULADO',
                    anulado_en = $anulado,
                    motivo_anulacion = $motivo,
                    actualizado_en = $actualizado
                WHERE id = $id AND estado IN ('PLANIFICADO', 'CONFIRMADO');
                """)
            .WithParameters(
                ("$id", expenseId.ToString()),
                ("$anulado", cancelledAt.ToStorage()),
                ("$motivo", reason),
                ("$actualizado", cancelledAt.ToStorage()));

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
    }

    public async Task<IReadOnlyList<Expense>> GetHistoryAsync(
        SqliteConnection connection,
        ExpenseHistoryRequest request,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                null,
                $"""
                {SelectExpenses}
                WHERE ($desde IS NULL OR {EffectiveDate} >= $desde)
                  AND ($hasta IS NULL OR {EffectiveDate} <= $hasta)
                  AND ($categoria IS NULL OR categoria_id = $categoria)
                  AND ($estado IS NULL OR estado = $estado)
                ORDER BY {EffectiveDate} DESC, creado_en DESC
                LIMIT $limite OFFSET $desplazamiento;
                """)
            .WithParameters(
                ("$desde", request.From?.ToStorage()),
                ("$hasta", request.To?.ToStorage()),
                ("$categoria", request.CategoryId?.ToString()),
                ("$estado", request.Status is null ? null : SqliteEnumCodes.ToCode(request.Status.Value)),
                ("$limite", request.Page.PageSize),
                ("$desplazamiento", (request.Page.Page - 1) * request.Page.PageSize));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        var expenses = new List<Expense>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            expenses.Add(Map(reader));
        }

        return expenses;
    }

    public async Task<int> CountHistoryAsync(
        SqliteConnection connection,
        ExpenseHistoryRequest request,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                null,
                $"""
                SELECT COUNT(*)
                FROM gastos
                WHERE ($desde IS NULL OR {EffectiveDate} >= $desde)
                  AND ($hasta IS NULL OR {EffectiveDate} <= $hasta)
                  AND ($categoria IS NULL OR categoria_id = $categoria)
                  AND ($estado IS NULL OR estado = $estado);
                """)
            .WithParameters(
                ("$desde", request.From?.ToStorage()),
                ("$hasta", request.To?.ToStorage()),
                ("$categoria", request.CategoryId?.ToString()),
                ("$estado", request.Status is null ? null : SqliteEnumCodes.ToCode(request.Status.Value)));

        return Convert.ToInt32(
            await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
    }

    public async Task InsertCorrectionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        ExpenseCorrection correction,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                INSERT INTO gastos_historial
                    (id, gasto_id, concepto_anterior, concepto_nuevo, monto_anterior,
                     monto_nuevo, categoria_anterior_id, categoria_nueva_id,
                     fecha_anterior, fecha_nueva, modificado_en)
                VALUES
                    ($id, $gasto, $conceptoAnterior, $conceptoNuevo, $montoAnterior,
                     $montoNuevo, $categoriaAnterior, $categoriaNueva,
                     $fechaAnterior, $fechaNueva, $modificado);
                """)
            .WithParameters(
                ("$id", correction.Id.ToString()),
                ("$gasto", correction.ExpenseId.ToString()),
                ("$conceptoAnterior", correction.PreviousConcept),
                ("$conceptoNuevo", correction.NewConcept),
                ("$montoAnterior", correction.PreviousAmountCents),
                ("$montoNuevo", correction.NewAmountCents),
                ("$categoriaAnterior", correction.PreviousCategoryId?.ToString()),
                ("$categoriaNueva", correction.NewCategoryId?.ToString()),
                ("$fechaAnterior", correction.PreviousDate?.ToStorage()),
                ("$fechaNueva", correction.NewDate?.ToStorage()),
                ("$modificado", correction.ModifiedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<ExpenseCorrection>> GetCorrectionsAsync(
        SqliteConnection connection,
        Guid expenseId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                null,
                """
                SELECT id, gasto_id, concepto_anterior, concepto_nuevo, monto_anterior,
                       monto_nuevo, categoria_anterior_id, categoria_nueva_id,
                       fecha_anterior, fecha_nueva, modificado_en
                FROM gastos_historial
                WHERE gasto_id = $gasto
                ORDER BY modificado_en DESC;
                """)
            .WithParameters(("$gasto", expenseId.ToString()));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        var corrections = new List<ExpenseCorrection>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            corrections.Add(new ExpenseCorrection
            {
                Id = reader.GetGuid("id"),
                ExpenseId = reader.GetGuid("gasto_id"),
                PreviousConcept = reader.GetNullableText("concepto_anterior"),
                NewConcept = reader.GetNullableText("concepto_nuevo"),
                PreviousAmountCents = reader.GetNullableCents("monto_anterior"),
                NewAmountCents = reader.GetNullableCents("monto_nuevo"),
                PreviousCategoryId = reader.GetNullableGuid("categoria_anterior_id"),
                NewCategoryId = reader.GetNullableGuid("categoria_nueva_id"),
                PreviousDate = reader.GetNullableDateOnly("fecha_anterior"),
                NewDate = reader.GetNullableDateOnly("fecha_nueva"),
                ModifiedAt = reader.GetTimestamp("modificado_en")
            });
        }

        return corrections;
    }

    private static Expense Map(SqliteDataReader reader) => new()
    {
        Id = reader.GetGuid("id"),
        Concept = reader.GetText("concepto"),
        CategoryId = reader.GetGuid("categoria_id"),
        OperationId = reader.GetGuid("operacion_id"),
        AmountCents = reader.GetCents("monto_centavos"),
        PlannedDate = reader.GetDateOnly("fecha_prevista"),
        ActualDate = reader.GetNullableDateOnly("fecha_real"),
        Status = SqliteEnumCodes.ParseExpenseStatus(reader.GetText("estado")),
        CancelledAt = reader.GetNullableTimestamp("anulado_en"),
        CancellationReason = reader.GetNullableText("motivo_anulacion"),
        CreatedAt = reader.GetTimestamp("creado_en"),
        UpdatedAt = reader.GetTimestamp("actualizado_en")
    };
}
