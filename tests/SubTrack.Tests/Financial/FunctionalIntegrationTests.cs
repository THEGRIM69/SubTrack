using Microsoft.Data.Sqlite;
using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Enums;
using SubTrack.Core.Results;
using SubTrack.Infrastructure;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Tests.Financial;

/// <summary>
/// Pruebas de integración funcional entre módulos: Suscripciones, Cobros, Pagos, Gastos, Ingresos y Dashboard.
/// Valida la consistencia de datos de extremo a extremo contra una base SQLite real.
/// </summary>
public sealed class FunctionalIntegrationTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 10, 28);
    private static readonly DateOnly CurrentMonth = new(2026, 10, 1);
    private static readonly DateOnly NextMonth = new(2026, 11, 1);

    private readonly string _tempDirectory;
    private readonly SubTrackDatabaseOptions _options;

    public FunctionalIntegrationTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "SubTrack.FunctionalTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);

        _options = new SubTrackDatabaseOptions
        {
            DatabasePath = Path.Combine(_tempDirectory, "test_integracion.db"),
            BusyTimeoutMilliseconds = 5_000
        };
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        try
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        catch (IOException)
        {
        }
    }

    private async Task<SubTrackServices> CreateServicesAsync(DateOnly? today = null)
    {
        var initializer = new SqliteDatabaseInitializer(_options);
        await initializer.InitializeAsync();

        var migrator = new SqliteDatabaseMigrator(_options);
        var migration = await migrator.MigrateAsync();

        Assert.True(migration.Success);

        return new SubTrackServices(_options, new FixedTimeProvider(today ?? Today));
    }

    [Fact]
    public async Task FlujoIntegral_ExtremoAExtremo_ConsistenciaTotalEntreModulos()
    {
        var services = await CreateServicesAsync();

        // 1. Configurar saldo inicial de $1,000.00
        var openingResult = await services.OpeningBalance.SetAsync(
            new SetOpeningBalanceRequest(100_000, new DateOnly(2026, 10, 1)));
        Assert.True(openingResult.IsSuccess);

        // Verificar Dashboard tras saldo inicial
        var dash0 = await services.Dashboard.GetAsync(new GetDashboardRequest(CurrentMonth));
        Assert.True(dash0.IsSuccess);
        Assert.Equal(100_000, dash0.Value.OpeningBalanceCents);
        Assert.Equal(100_000, dash0.Value.CurrentBalanceCents);
        Assert.Equal(100_000, dash0.Value.ProjectedSavingsCents);
        Assert.Equal(0, dash0.Value.ActiveSubscriptions);
        Assert.Empty(dash0.Value.UpcomingCharges);

        // 2. Dar de alta una suscripción mensual de $15.00 con primer cobro el 28/10/2026
        var subResult = await services.Subscriptions.CreateAsync(new CreateSubscriptionRequest(
            "Netflix Standard",
            1_500,
            AmountType.Fixed,
            BillingFrequency.Monthly,
            new DateOnly(2026, 10, 28),
            Guid.NewGuid()));
        Assert.True(subResult.IsSuccess);

        // Generar cobros hasta fin de mes (31/10/2026)
        var genResult = await services.Charges.GenerateAsync(new GenerateChargesRequest(
            subResult.Value.Id,
            new DateOnly(2026, 10, 31)));
        Assert.True(genResult.IsSuccess);
        Assert.Single(genResult.Value);
        var charge = genResult.Value[0];
        Assert.Equal(new DateOnly(2026, 10, 28), charge.ScheduledDate);
        Assert.Equal(1_500, charge.AmountCents);
        Assert.Equal(ScheduledChargeStatus.Pending, charge.Status);

        // Verificar Dashboard con cobro programado pendiente
        var dash1 = await services.Dashboard.GetAsync(new GetDashboardRequest(CurrentMonth));
        Assert.True(dash1.IsSuccess);
        Assert.Equal(1, dash1.Value.ActiveSubscriptions);
        Assert.Single(dash1.Value.UpcomingCharges);
        Assert.Empty(dash1.Value.OverdueCharges);
        Assert.Equal(100_000, dash1.Value.CurrentBalanceCents); // Saldo actual no baja porque aún no se paga
        Assert.Equal(100_000 - 1_500, dash1.Value.ProjectedSavingsCents); // Ahorro proyectado sí descuenta el cobro pendiente
        Assert.Equal(0, dash1.Value.SubscriptionPaymentsCents);

        // 3. Registrar el pago del cobro de Netflix ($15.00)
        var payResult = await services.Payments.RegisterAsync(new RegisterPaymentRequest(
            charge.Id,
            new DateOnly(2026, 10, 28),
            Guid.NewGuid()));
        Assert.True(payResult.IsSuccess);
        Assert.StartsWith("CMP-2026-", payResult.Value.VoucherNumber);
        Assert.Equal(1_500, payResult.Value.AmountCents);

        // Verificar que el cobro pasó a pagado
        var pendingCharges = await services.Charges.GetPendingAsync();
        Assert.True(pendingCharges.IsSuccess);
        Assert.Empty(pendingCharges.Value);

        // Verificar Dashboard tras el pago
        var dash2 = await services.Dashboard.GetAsync(new GetDashboardRequest(CurrentMonth));
        Assert.True(dash2.IsSuccess);
        Assert.Empty(dash2.Value.UpcomingCharges);
        Assert.Equal(1_500, dash2.Value.SubscriptionPaymentsCents);
        Assert.Equal(1_500, dash2.Value.TotalExpensesCents);
        Assert.Equal(100_000 - 1_500, dash2.Value.CurrentBalanceCents); // Saldo actual baja por el pago
        Assert.Equal(100_000 - 1_500, dash2.Value.ProjectedSavingsCents);

        // 4. Obtener la categoría existente (Alimentación) y registrar un gasto confirmado de $50.00
        var categoriesResult = await services.Categories.GetAllAsync(includeInactive: false);
        Assert.True(categoriesResult.IsSuccess);
        var foodCategory = categoriesResult.Value.First(c => c.Name == "Alimentación");

        var expResult = await services.Expenses.CreateAsync(new CreateExpenseRequest(
            "Supermercado",
            foodCategory.Id,
            5_000,
            new DateOnly(2026, 10, 28),
            new DateOnly(2026, 10, 28), // Confirmado hoy
            Guid.NewGuid()));
        Assert.True(expResult.IsSuccess);
        Assert.Equal(ExpenseStatus.Confirmed, expResult.Value.Status);

        // Verificar Dashboard tras gasto confirmado
        var dash3 = await services.Dashboard.GetAsync(new GetDashboardRequest(CurrentMonth));
        Assert.True(dash3.IsSuccess);
        Assert.Equal(5_000, dash3.Value.ExpensesCents);
        Assert.Equal(1_500, dash3.Value.SubscriptionPaymentsCents);
        Assert.Equal(6_500, dash3.Value.TotalExpensesCents);
        Assert.Equal(100_000 - 6_500, dash3.Value.CurrentBalanceCents); // 93,500
        Assert.Equal(100_000 - 6_500, dash3.Value.ProjectedSavingsCents);

        // 5. Registrar un ingreso confirmado de $200.00
        var incResult = await services.Incomes.CreateAsync(new CreateIncomeRequest(
            "Honorarios Consultoría",
            20_000,
            IncomeType.Extraordinary,
            null,
            new DateOnly(2026, 10, 28), // Recibido hoy
            null,
            Guid.NewGuid()));
        Assert.True(incResult.IsSuccess);
        Assert.Equal(IncomeStatus.Confirmed, incResult.Value.Status);

        // Verificar Dashboard tras ingreso confirmado
        var dash4 = await services.Dashboard.GetAsync(new GetDashboardRequest(CurrentMonth));
        Assert.True(dash4.IsSuccess);
        Assert.Equal(20_000, dash4.Value.IncomeCents);
        Assert.Equal(6_500, dash4.Value.TotalExpensesCents);
        Assert.Equal(100_000 + 20_000 - 6_500, dash4.Value.CurrentBalanceCents); // 113,500
        Assert.Equal(100_000 + 20_000 - 6_500, dash4.Value.ProjectedSavingsCents);

        // 6. Navegación de meses: consultar mes siguiente (Noviembre 2026)
        var dashNov = await services.Dashboard.GetAsync(new GetDashboardRequest(NextMonth));
        Assert.True(dashNov.IsSuccess);
        Assert.Equal(0, dashNov.Value.IncomeCents); // En noviembre no hay ingresos confirmados
        Assert.Equal(0, dashNov.Value.ExpensesCents); // En noviembre no hay gastos confirmados
        Assert.Equal(0, dashNov.Value.SubscriptionPaymentsCents);
        Assert.Equal(0, dashNov.Value.TotalExpensesCents);
        // Saldo inicial acumulado al cierre de noviembre incluye los movimientos acumulados hasta noviembre
        Assert.Equal(113_500, dashNov.Value.CurrentBalanceCents);
    }

    [Fact]
    public async Task SuscripcionRecienCreada_SinGenerarCobros_NoImpactaCobrosNiProyeccionDashboard()
    {
        var services = await CreateServicesAsync();

        // Crear suscripción sin InitialPendingCharges
        var subResult = await services.Subscriptions.CreateAsync(new CreateSubscriptionRequest(
            "Spotify",
            1_000,
            AmountType.Fixed,
            BillingFrequency.Monthly,
            new DateOnly(2026, 10, 28),
            Guid.NewGuid()));
        Assert.True(subResult.IsSuccess);

        // Sin llamar a GenerateAsync:
        // Cobros pendientes: 0
        var pending = await services.Charges.GetPendingAsync();
        Assert.True(pending.IsSuccess);
        Assert.Empty(pending.Value);

        // Dashboard: activa = 1, pero 0 cobros por venir y ahorro proyectado = 0
        var dash = await services.Dashboard.GetAsync(new GetDashboardRequest(CurrentMonth));
        Assert.True(dash.IsSuccess);
        Assert.Equal(1, dash.Value.ActiveSubscriptions);
        Assert.Empty(dash.Value.UpcomingCharges);
        Assert.Equal(0, dash.Value.ProjectedSavingsCents);
    }

    [Fact]
    public async Task SuscripcionConInicioAnterior_GenerarCobros_DescartaFechasAnterioresAHoy()
    {
        var services = await CreateServicesAsync();

        // Suscripción que inició el 01/10/2026 (hoy es 28/10/2026)
        var subResult = await services.Subscriptions.CreateAsync(new CreateSubscriptionRequest(
            "Gimnasio",
            5_000,
            AmountType.Fixed,
            BillingFrequency.Monthly,
            new DateOnly(2026, 10, 1),
            Guid.NewGuid()));
        Assert.True(subResult.IsSuccess);

        // Intentar generar cobros hasta el 31/10/2026
        var gen = await services.Charges.GenerateAsync(new GenerateChargesRequest(
            subResult.Value.Id,
            new DateOnly(2026, 10, 31)));
        Assert.True(gen.IsSuccess);

        // EVIDENCIA CLAVE:
        // Como 'today' es 28/10/2026 y la regla del planificador parte de 'today',
        // el cobro del 01/10/2026 NO se genera automáticamente (Count == 0),
        // porque los cobros anteriores a hoy requieren importación explícita (InitialPendingCharges).
        Assert.Empty(gen.Value);
    }
}
