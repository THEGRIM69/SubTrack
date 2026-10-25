using Microsoft.Data.Sqlite;
using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Entities;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Infrastructure.Persistence.Repositories;

/// <summary>
/// Acceso a ingresos, plantillas recurrentes y su historial de correcciones.
/// </summary>
internal sealed class IncomeRepository
{
    private const string SelectIncomes = """
        SELECT id, concepto, plantilla_id, operacion_id, monto_centavos, tipo,
               fecha_prevista, fecha_real, estado, anulado_en, motivo_anulacion,
               creado_en, actualizado_en
        FROM ingresos
        """;

    private const string EffectiveDate = "COALESCE(fecha_real, fecha_prevista)";

    public async Task<Income?> GetByIdAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid incomeId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(transaction, $"{SelectIncomes} WHERE id = $id;")
            .WithParameters(("$id", incomeId.ToString()));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? Map(reader)
            : null;
    }

    public async Task<Income?> GetByOperationIdAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(transaction, $"{SelectIncomes} WHERE operacion_id = $operacion;")
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
        Income income,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                INSERT INTO ingresos
                    (id, concepto, plantilla_id, operacion_id, monto_centavos, tipo,
                     fecha_prevista, fecha_real, estado, anulado_en, motivo_anulacion,
                     creado_en, actualizado_en)
                VALUES
                    ($id, $concepto, $plantilla, $operacion, $monto, $tipo,
                     $prevista, $real, $estado, $anulado, $motivo, $creado, $actualizado);
                """)
            .WithParameters(
                ("$id", income.Id.ToString()),
                ("$concepto", income.Concept),
                ("$plantilla", income.TemplateId?.ToString()),
                ("$operacion", income.OperationId.ToString()),
                ("$monto", income.AmountCents),
                ("$tipo", SqliteEnumCodes.ToCode(income.Type)),
                ("$prevista", income.PlannedDate.ToStorage()),
                ("$real", income.ActualDate?.ToStorage()),
                ("$estado", SqliteEnumCodes.ToCode(income.Status)),
                ("$anulado", income.CancelledAt?.ToStorage()),
                ("$motivo", income.CancellationReason),
                ("$creado", income.CreatedAt.ToStorage()),
                ("$actualizado", income.UpdatedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Income income,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                UPDATE ingresos
                SET concepto = $concepto,
                    monto_centavos = $monto,
                    fecha_prevista = $prevista,
                    actualizado_en = $actualizado
                WHERE id = $id;
                """)
            .WithParameters(
                ("$id", income.Id.ToString()),
                ("$concepto", income.Concept),
                ("$monto", income.AmountCents),
                ("$prevista", income.PlannedDate.ToStorage()),
                ("$actualizado", income.UpdatedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<bool> ConfirmAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid incomeId,
        DateOnly actualDate,
        DateTimeOffset updatedAt,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                UPDATE ingresos
                SET estado = 'CONFIRMADO',
                    fecha_real = $real,
                    actualizado_en = $actualizado
                WHERE id = $id AND estado = 'PREVISTO';
                """)
            .WithParameters(
                ("$id", incomeId.ToString()),
                ("$real", actualDate.ToStorage()),
                ("$actualizado", updatedAt.ToStorage()));

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
    }

    public async Task<bool> CancelAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid incomeId,
        DateTimeOffset cancelledAt,
        string reason,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                UPDATE ingresos
                SET estado = 'ANULADO',
                    anulado_en = $anulado,
                    motivo_anulacion = $motivo,
                    actualizado_en = $actualizado
                WHERE id = $id AND estado IN ('PREVISTO', 'CONFIRMADO');
                """)
            .WithParameters(
                ("$id", incomeId.ToString()),
                ("$anulado", cancelledAt.ToStorage()),
                ("$motivo", reason),
                ("$actualizado", cancelledAt.ToStorage()));

        return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false) == 1;
    }

    public async Task<bool> ExistsPlannedAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        Guid templateId,
        DateOnly date,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                SELECT 1 FROM ingresos
                WHERE plantilla_id = $plantilla AND fecha_prevista = $fecha
                LIMIT 1;
                """)
            .WithParameters(
                ("$plantilla", templateId.ToString()),
                ("$fecha", date.ToStorage()));

        return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) is not null;
    }

    public async Task<IReadOnlyList<Income>> GetHistoryAsync(
        SqliteConnection connection,
        IncomeHistoryRequest request,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                null,
                $"""
                {SelectIncomes}
                WHERE ($desde IS NULL OR {EffectiveDate} >= $desde)
                  AND ($hasta IS NULL OR {EffectiveDate} <= $hasta)
                  AND ($estado IS NULL OR estado = $estado)
                ORDER BY {EffectiveDate} DESC, creado_en DESC
                LIMIT $limite OFFSET $desplazamiento;
                """)
            .WithParameters(
                ("$desde", request.From?.ToStorage()),
                ("$hasta", request.To?.ToStorage()),
                ("$estado", request.Status is null ? null : SqliteEnumCodes.ToCode(request.Status.Value)),
                ("$limite", request.Page.PageSize),
                ("$desplazamiento", (request.Page.Page - 1) * request.Page.PageSize));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        var incomes = new List<Income>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            incomes.Add(Map(reader));
        }

        return incomes;
    }

    public async Task<int> CountHistoryAsync(
        SqliteConnection connection,
        IncomeHistoryRequest request,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                null,
                $"""
                SELECT COUNT(*)
                FROM ingresos
                WHERE ($desde IS NULL OR {EffectiveDate} >= $desde)
                  AND ($hasta IS NULL OR {EffectiveDate} <= $hasta)
                  AND ($estado IS NULL OR estado = $estado);
                """)
            .WithParameters(
                ("$desde", request.From?.ToStorage()),
                ("$hasta", request.To?.ToStorage()),
                ("$estado", request.Status is null ? null : SqliteEnumCodes.ToCode(request.Status.Value)));

        return Convert.ToInt32(
            await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
    }

    public async Task InsertCorrectionAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        IncomeCorrection correction,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                INSERT INTO ingresos_historial
                    (id, ingreso_id, concepto_anterior, concepto_nuevo, monto_anterior,
                     monto_nuevo, fecha_anterior, fecha_nueva, modificado_en)
                VALUES
                    ($id, $ingreso, $conceptoAnterior, $conceptoNuevo, $montoAnterior,
                     $montoNuevo, $fechaAnterior, $fechaNueva, $modificado);
                """)
            .WithParameters(
                ("$id", correction.Id.ToString()),
                ("$ingreso", correction.IncomeId.ToString()),
                ("$conceptoAnterior", correction.PreviousConcept),
                ("$conceptoNuevo", correction.NewConcept),
                ("$montoAnterior", correction.PreviousAmountCents),
                ("$montoNuevo", correction.NewAmountCents),
                ("$fechaAnterior", correction.PreviousDate?.ToStorage()),
                ("$fechaNueva", correction.NewDate?.ToStorage()),
                ("$modificado", correction.ModifiedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<IncomeCorrection>> GetCorrectionsAsync(
        SqliteConnection connection,
        Guid incomeId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                null,
                """
                SELECT id, ingreso_id, concepto_anterior, concepto_nuevo, monto_anterior,
                       monto_nuevo, fecha_anterior, fecha_nueva, modificado_en
                FROM ingresos_historial
                WHERE ingreso_id = $ingreso
                ORDER BY modificado_en DESC;
                """)
            .WithParameters(("$ingreso", incomeId.ToString()));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        var corrections = new List<IncomeCorrection>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            corrections.Add(new IncomeCorrection
            {
                Id = reader.GetGuid("id"),
                IncomeId = reader.GetGuid("ingreso_id"),
                PreviousConcept = reader.GetNullableText("concepto_anterior"),
                NewConcept = reader.GetNullableText("concepto_nuevo"),
                PreviousAmountCents = reader.GetNullableCents("monto_anterior"),
                NewAmountCents = reader.GetNullableCents("monto_nuevo"),
                PreviousDate = reader.GetNullableDateOnly("fecha_anterior"),
                NewDate = reader.GetNullableDateOnly("fecha_nueva"),
                ModifiedAt = reader.GetTimestamp("modificado_en")
            });
        }

        return corrections;
    }

    public async Task<RecurringIncomeTemplate?> GetTemplateAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid templateId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                SELECT id, concepto, operacion_id, monto_centavos, frecuencia, dia_ancla,
                       fecha_inicio, activa, creado_en, actualizado_en
                FROM ingresos_plantillas
                WHERE id = $id;
                """)
            .WithParameters(("$id", templateId.ToString()));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? MapTemplate(reader)
            : null;
    }

    public async Task<IReadOnlyList<RecurringIncomeTemplate>> GetTemplatesAsync(
        SqliteConnection connection,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand(
            null,
            """
            SELECT id, concepto, operacion_id, monto_centavos, frecuencia, dia_ancla,
                   fecha_inicio, activa, creado_en, actualizado_en
            FROM ingresos_plantillas
            WHERE $incluirInactivas = 1 OR activa = 1
            ORDER BY concepto ASC;
            """);

        command.Parameters.AddWithValue("$incluirInactivas", includeInactive ? 1 : 0);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        var templates = new List<RecurringIncomeTemplate>();

        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
        {
            templates.Add(MapTemplate(reader));
        }

        return templates;
    }

    public async Task<RecurringIncomeTemplate?> GetTemplateByOperationIdAsync(
        SqliteConnection connection,
        SqliteTransaction? transaction,
        Guid operationId,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                SELECT id, concepto, operacion_id, monto_centavos, frecuencia, dia_ancla,
                       fecha_inicio, activa, creado_en, actualizado_en
                FROM ingresos_plantillas
                WHERE operacion_id = $operacion;
                """)
            .WithParameters(("$operacion", operationId.ToString()));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken)
            .ConfigureAwait(false);

        return await reader.ReadAsync(cancellationToken).ConfigureAwait(false)
            ? MapTemplate(reader)
            : null;
    }

    public async Task InsertTemplateAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        RecurringIncomeTemplate template,
        CancellationToken cancellationToken)
    {
        await using var command = connection
            .CreateCommand(
                transaction,
                """
                INSERT INTO ingresos_plantillas
                    (id, concepto, operacion_id, monto_centavos, frecuencia, dia_ancla,
                     fecha_inicio, activa, creado_en, actualizado_en)
                VALUES
                    ($id, $concepto, $operacion, $monto, $frecuencia, $ancla,
                     $inicio, $activa, $creado, $actualizado);
                """)
            .WithParameters(
                ("$id", template.Id.ToString()),
                ("$concepto", template.Concept),
                ("$operacion", template.OperationId.ToString()),
                ("$monto", template.AmountCents),
                ("$frecuencia", SqliteEnumCodes.ToCode(template.Frequency)),
                ("$ancla", template.AnchorDay),
                ("$inicio", template.StartDate.ToStorage()),
                ("$activa", template.IsActive ? 1 : 0),
                ("$creado", template.CreatedAt.ToStorage()),
                ("$actualizado", template.UpdatedAt.ToStorage()));

        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
    }

    private static Income Map(SqliteDataReader reader) => new()
    {
        Id = reader.GetGuid("id"),
        Concept = reader.GetText("concepto"),
        TemplateId = reader.GetNullableGuid("plantilla_id"),
        OperationId = reader.GetGuid("operacion_id"),
        AmountCents = reader.GetCents("monto_centavos"),
        Type = SqliteEnumCodes.ParseIncomeType(reader.GetText("tipo")),
        PlannedDate = reader.GetDateOnly("fecha_prevista"),
        ActualDate = reader.GetNullableDateOnly("fecha_real"),
        Status = SqliteEnumCodes.ParseIncomeStatus(reader.GetText("estado")),
        CancelledAt = reader.GetNullableTimestamp("anulado_en"),
        CancellationReason = reader.GetNullableText("motivo_anulacion"),
        CreatedAt = reader.GetTimestamp("creado_en"),
        UpdatedAt = reader.GetTimestamp("actualizado_en")
    };

    private static RecurringIncomeTemplate MapTemplate(SqliteDataReader reader) => new()
    {
        Id = reader.GetGuid("id"),
        Concept = reader.GetText("concepto"),
        OperationId = reader.GetGuid("operacion_id"),
        AmountCents = reader.GetCents("monto_centavos"),
        Frequency = SqliteEnumCodes.ParseIncomeType(reader.GetText("frecuencia")),
        AnchorDay = reader.GetInt32("dia_ancla"),
        StartDate = reader.GetDateOnly("fecha_inicio"),
        IsActive = reader.GetBoolean("activa"),
        CreatedAt = reader.GetTimestamp("creado_en"),
        UpdatedAt = reader.GetTimestamp("actualizado_en")
    };
}
