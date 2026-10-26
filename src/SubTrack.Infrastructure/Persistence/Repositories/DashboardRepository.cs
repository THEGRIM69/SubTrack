using Microsoft.Data.Sqlite;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Infrastructure.Persistence.Repositories;

/// <summary>
/// F0-11 — Consultas agregadas del Dashboard. No aplica reglas: solo suma
/// movimientos vigentes para que Core calcule el resultado final.
/// </summary>
internal sealed class DashboardRepository
{
    public async Task<long> SumConfirmedIncomeAsync(
        SqliteConnection connection,
        DateOnly until,
        CancellationToken cancellationToken) =>
        await ScalarAsync(
            connection,
            """
            SELECT COALESCE(SUM(monto_centavos), 0)
            FROM ingresos
            WHERE estado = 'CONFIRMADO' AND fecha_real <= $hasta;
            """,
            until,
            cancellationToken).ConfigureAwait(false);

    public async Task<long> SumConfirmedExpensesAsync(
        SqliteConnection connection,
        DateOnly until,
        CancellationToken cancellationToken) =>
        await ScalarAsync(
            connection,
            """
            SELECT COALESCE(SUM(monto_centavos), 0)
            FROM gastos
            WHERE estado = 'CONFIRMADO' AND fecha_real <= $hasta;
            """,
            until,
            cancellationToken).ConfigureAwait(false);

    public async Task<long> SumConfirmedPaymentsAsync(
        SqliteConnection connection,
        DateOnly until,
        CancellationToken cancellationToken) =>
        await ScalarAsync(
            connection,
            """
            SELECT COALESCE(SUM(monto_centavos), 0)
            FROM pagos
            WHERE estado = 'VIGENTE' AND fecha_pago <= $hasta;
            """,
            until,
            cancellationToken).ConfigureAwait(false);

    public async Task<long> SumPlannedIncomeAsync(
        SqliteConnection connection,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        await ScalarRangeAsync(
            connection,
            """
            SELECT COALESCE(SUM(monto_centavos), 0)
            FROM ingresos
            WHERE estado = 'PREVISTO' AND fecha_prevista BETWEEN $desde AND $hasta;
            """,
            from,
            to,
            cancellationToken).ConfigureAwait(false);

    public async Task<long> SumPlannedExpensesAsync(
        SqliteConnection connection,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        await ScalarRangeAsync(
            connection,
            """
            SELECT COALESCE(SUM(monto_centavos), 0)
            FROM gastos
            WHERE estado = 'PLANIFICADO' AND fecha_prevista BETWEEN $desde AND $hasta;
            """,
            from,
            to,
            cancellationToken).ConfigureAwait(false);

    public async Task<long> SumPendingChargesAsync(
        SqliteConnection connection,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken) =>
        await ScalarRangeAsync(
            connection,
            """
            SELECT COALESCE(SUM(monto_centavos), 0)
            FROM cobros_programados
            WHERE estado = 'PENDIENTE' AND fecha_programada BETWEEN $desde AND $hasta;
            """,
            from,
            to,
            cancellationToken).ConfigureAwait(false);

    public async Task<int> CountActiveSubscriptionsAsync(
        SqliteConnection connection,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand(
            null,
            "SELECT COUNT(*) FROM suscripciones WHERE activa = 1;");

        return Convert.ToInt32(
            await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
    }

    private static async Task<long> ScalarAsync(
        SqliteConnection connection,
        string sql,
        DateOnly until,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand(null, sql)
            .WithParameters(("$hasta", until.ToStorage()));

        return Convert.ToInt64(
            await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
    }

    private static async Task<long> ScalarRangeAsync(
        SqliteConnection connection,
        string sql,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand(null, sql)
            .WithParameters(("$desde", from.ToStorage()), ("$hasta", to.ToStorage()));

        return Convert.ToInt64(
            await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false));
    }
}
