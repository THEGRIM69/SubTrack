using Microsoft.Data.Sqlite;
using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Enums;
using SubTrack.Core.Results;
using SubTrack.Infrastructure;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Tests.Financial;

/// <summary>
/// F6 y F7 — Gastos, ingresos, categorías, saldo inicial y Dashboard.
/// </summary>
public sealed class ExpenseIncomeDashboardTests : IDisposable
{
    private static readonly Guid Food = Guid.Parse("11111111-1111-4111-8111-111111111111");
    private static readonly Guid Transport = Guid.Parse("22222222-2222-4222-8222-222222222222");

    private static readonly DateOnly Today = new(2026, 10, 25);
    private static readonly DateOnly Month = new(2026, 10, 1);

    private readonly string _tempDirectory;
    private readonly SubTrackDatabaseOptions _options;

    public ExpenseIncomeDashboardTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "SubTrack.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);

        _options = new SubTrackDatabaseOptions
        {
            DatabasePath = Path.Combine(_tempDirectory, "gastos.db"),
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
            // Carpeta temporal; el sistema la liberará.
        }
    }

    [Fact]
    public async Task Gasto_PlanificadoNoCuentaHastaConfirmarse()
    {
        var services = await CreateServicesAsync();

        var created = await services.Expenses.CreateAsync(
            new CreateExpenseRequest(
                "Compra semanal",
                Food,
                4_000,
                new DateOnly(2026, 10, 27),
                null,
                Guid.NewGuid()));

        Assert.True(created.IsSuccess, Describe(created));
        Assert.Equal(ExpenseStatus.Planned, created.Value.Status);
        Assert.Null(created.Value.ActualDate);

        var dashboard = await services.Dashboard.GetAsync(new GetDashboardRequest(Month));

        Assert.True(dashboard.IsSuccess, Describe(dashboard));
        Assert.Equal(0, dashboard.Value.ExpensesCents);
        Assert.Equal(-4_000, dashboard.Value.ProjectedSavingsCents - dashboard.Value.CurrentBalanceCents);
    }

    [Fact]
    public async Task Gasto_ConFechaReal_QuedaConfirmadoYCuentaUnaSolaVez()
    {
        var services = await CreateServicesAsync();

        var created = await services.Expenses.CreateAsync(
            new CreateExpenseRequest(
                "Compra semanal",
                Food,
                4_000,
                new DateOnly(2026, 10, 20),
                null,
                Guid.NewGuid()));

        var confirmed = await services.Expenses.ConfirmAsync(
            new ConfirmExpenseRequest(created.Value.Id, new DateOnly(2026, 10, 21)));

        Assert.True(confirmed.IsSuccess, Describe(confirmed));
        Assert.Equal(ExpenseStatus.Confirmed, confirmed.Value.Status);
        Assert.Equal(new DateOnly(2026, 10, 21), confirmed.Value.ActualDate);
        Assert.Equal(new DateOnly(2026, 10, 20), confirmed.Value.PlannedDate);

        var dashboard = await services.Dashboard.GetAsync(new GetDashboardRequest(Month));

        Assert.Equal(4_000, dashboard.Value.ExpensesCents);
        Assert.Equal(4_000, dashboard.Value.TotalExpensesCents);
        Assert.Equal(-4_000, dashboard.Value.CurrentBalanceCents);
    }

    [Fact]
    public async Task Gasto_ConMismaOperacion_NoDuplica()
    {
        var services = await CreateServicesAsync();
        var operationId = Guid.NewGuid();

        var request = new CreateExpenseRequest(
            "Café",
            Food,
            500,
            Today,
            null,
            operationId);

        var first = await services.Expenses.CreateAsync(request);
        var second = await services.Expenses.CreateAsync(request);

        Assert.Equal(first.Value.Id, second.Value.Id);

        var history = await services.Expenses.GetHistoryAsync(new ExpenseHistoryRequest(new PageRequest()));

        Assert.Single(history.Value.Items);
    }

    [Fact]
    public async Task Gasto_Edicion_RegistraCorreccionConValoresAnteriores()
    {
        var services = await CreateServicesAsync();

        var created = await services.Expenses.CreateAsync(
            new CreateExpenseRequest("Cine", Food, 1_000, Today, null, Guid.NewGuid()));

        var updated = await services.Expenses.UpdateAsync(
            new UpdateExpenseRequest(
                created.Value.Id,
                "Cine y palomitas",
                Transport,
                1_500,
                Today));

        Assert.True(updated.IsSuccess, Describe(updated));
        Assert.Equal(1_500, updated.Value.AmountCents);

        var corrections = await services.Expenses.GetCorrectionsAsync(created.Value.Id);

        Assert.True(corrections.IsSuccess, Describe(corrections));
        var correction = Assert.Single(corrections.Value);
        Assert.Equal("Cine", correction.PreviousConcept);
        Assert.Equal("Cine y palomitas", correction.NewConcept);
        Assert.Equal(1_000, correction.PreviousAmountCents);
        Assert.Equal(1_500, correction.NewAmountCents);
        Assert.Equal(Food, correction.PreviousCategoryId);
        Assert.Equal(Transport, correction.NewCategoryId);
    }

    [Fact]
    public async Task Gasto_Anulado_NoAdmiteEdicionYLaAnulacionEsIdempotente()
    {
        var services = await CreateServicesAsync();

        var created = await services.Expenses.CreateAsync(
            new CreateExpenseRequest("Cine", Food, 1_000, Today, null, Guid.NewGuid()));

        var cancelled = await services.Expenses.CancelAsync(
            new CancelExpenseRequest(created.Value.Id, "No se realizó"));

        Assert.True(cancelled.IsSuccess, Describe(cancelled));
        Assert.Equal(ExpenseStatus.Cancelled, cancelled.Value.Status);

        var again = await services.Expenses.CancelAsync(
            new CancelExpenseRequest(created.Value.Id, "No se realizó"));

        Assert.True(again.IsSuccess, Describe(again));
        Assert.Equal(ExpenseStatus.Cancelled, again.Value.Status);

        var update = await services.Expenses.UpdateAsync(
            new UpdateExpenseRequest(created.Value.Id, "Otro", Food, 2_000, Today));

        Assert.True(update.IsFailure);
        Assert.Equal(ErrorCodes.GastoYaAnulado, Assert.Single(update.Errors).Code);
    }

    [Fact]
    public async Task Gasto_Historial_FiltraPorCategoriaYEstado()
    {
        var services = await CreateServicesAsync();

        await services.Expenses.CreateAsync(
            new CreateExpenseRequest("Uno", Food, 1_000, Today, null, Guid.NewGuid()));

        await services.Expenses.CreateAsync(
            new CreateExpenseRequest("Dos", Transport, 2_000, Today, null, Guid.NewGuid()));

        await services.Expenses.CreateAsync(
            new CreateExpenseRequest("Tres", Transport, 3_000, Today, Today, Guid.NewGuid()));

        var food = await services.Expenses.GetHistoryAsync(
            new ExpenseHistoryRequest(new PageRequest(), CategoryId: Food));

        Assert.Single(food.Value.Items);
        Assert.Equal("Uno", food.Value.Items[0].Concept);

        var confirmed = await services.Expenses.GetHistoryAsync(
            new ExpenseHistoryRequest(new PageRequest(), Status: ExpenseStatus.Confirmed));

        Assert.Single(confirmed.Value.Items);
        Assert.Equal(3_000, confirmed.Value.Items[0].AmountCents);

        var firstPage = await services.Expenses.GetHistoryAsync(
            new ExpenseHistoryRequest(new PageRequest(Page: 1, PageSize: 2)));

        Assert.Equal(3, firstPage.Value.TotalCount);
        Assert.Equal(2, firstPage.Value.Items.Count);
        Assert.True(firstPage.Value.HasNextPage);
    }

    [Fact]
    public async Task Gasto_ConCategoriaInactiva_SeRechaza()
    {
        var services = await CreateServicesAsync();

        await services.Categories.DeactivateAsync(Transport);

        var result = await services.Expenses.CreateAsync(
            new CreateExpenseRequest("Taxi", Transport, 900, Today, null, Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.CategoriaInactiva, Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Categorias_NoPermitenNombresDuplicadosNiBorrarConGastos()
    {
        var services = await CreateServicesAsync();

        var created = await services.Categories.CreateAsync(
            new CreateExpenseCategoryRequest("Mascotas"));

        Assert.True(created.IsSuccess, Describe(created));

        var duplicate = await services.Categories.CreateAsync(
            new CreateExpenseCategoryRequest("  mascotas "));

        Assert.True(duplicate.IsFailure);
        Assert.Equal(ErrorCodes.CategoriaDuplicada, Assert.Single(duplicate.Errors).Code);

        await services.Expenses.CreateAsync(
            new CreateExpenseRequest(
                "Pienso",
                created.Value.Id,
                3_000,
                Today,
                null,
                Guid.NewGuid()));

        var blocked = await services.Categories.DeleteAsync(created.Value.Id);

        Assert.True(blocked.IsFailure);
        Assert.Equal(ErrorCodes.CategoriaConGastos, Assert.Single(blocked.Errors).Code);

        var deletable = await services.Categories.CreateAsync(
            new CreateExpenseCategoryRequest("Temporal"));
        var deleted = await services.Categories.DeleteAsync(deletable.Value.Id);

        Assert.True(deleted.IsSuccess);
    }

    [Fact]
    public async Task PlantillaMensual_GeneraIngresosPrevistosSinDuplicar()
    {
        var services = await CreateServicesAsync();

        var template = await services.Incomes.CreateTemplateAsync(
            new CreateIncomeTemplateRequest(
                "Sueldo",
                300_000,
                IncomeType.Monthly,
                30,
                new DateOnly(2026, 10, 1),
                Guid.NewGuid()));

        Assert.True(template.IsSuccess, Describe(template));

        var generated = await services.Incomes.GeneratePlannedAsync(
            new GeneratePlannedIncomesRequest(template.Value.Id, new DateOnly(2027, 1, 31)));

        Assert.True(generated.IsSuccess, Describe(generated));
        Assert.Equal(
            new[]
            {
                new DateOnly(2026, 10, 30),
                new DateOnly(2026, 11, 30),
                new DateOnly(2026, 12, 30),
                new DateOnly(2027, 1, 30)
            },
            generated.Value.Select(income => income.PlannedDate));

        Assert.All(generated.Value, income => Assert.Equal(IncomeStatus.Planned, income.Status));

        var again = await services.Incomes.GeneratePlannedAsync(
            new GeneratePlannedIncomesRequest(template.Value.Id, new DateOnly(2027, 1, 31)));

        Assert.Empty(again.Value);
    }

    [Fact]
    public async Task PlantillaQuincenal_GeneraDiaQuinceYUltimoDia()
    {
        var services = await CreateServicesAsync();

        var template = await services.Incomes.CreateTemplateAsync(
            new CreateIncomeTemplateRequest(
                "Quincena",
                150_000,
                IncomeType.Biweekly,
                31,
                new DateOnly(2026, 10, 1),
                Guid.NewGuid()));

        var generated = await services.Incomes.GeneratePlannedAsync(
            new GeneratePlannedIncomesRequest(template.Value.Id, new DateOnly(2026, 12, 31)));

        Assert.True(generated.IsSuccess, Describe(generated));
        Assert.Equal(
            new[]
            {
                new DateOnly(2026, 10, 31),
                new DateOnly(2026, 11, 15),
                new DateOnly(2026, 11, 30),
                new DateOnly(2026, 12, 15),
                new DateOnly(2026, 12, 31)
            },
            generated.Value.Select(income => income.PlannedDate));
    }

    [Fact]
    public async Task PlantillaQuincenal_RechazaDiaAnclaDistinto()
    {
        var services = await CreateServicesAsync();

        var result = await services.Incomes.CreateTemplateAsync(
            new CreateIncomeTemplateRequest(
                "Quincena",
                150_000,
                IncomeType.Biweekly,
                10,
                new DateOnly(2026, 10, 1),
                Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.DiasDeIngresoInvalidos, Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Plantillas_ListanOrdenadasYRespetanInactivas()
    {
        var services = await CreateServicesAsync();

        var zeta = await services.Incomes.CreateTemplateAsync(
            new CreateIncomeTemplateRequest(
                "Zeta",
                100_000,
                IncomeType.Monthly,
                5,
                new DateOnly(2026, 10, 1),
                Guid.NewGuid()));

        var alfa = await services.Incomes.CreateTemplateAsync(
            new CreateIncomeTemplateRequest(
                "Alfa",
                200_000,
                IncomeType.Monthly,
                10,
                new DateOnly(2026, 10, 1),
                Guid.NewGuid()));

        Assert.True(zeta.IsSuccess, Describe(zeta));
        Assert.True(alfa.IsSuccess, Describe(alfa));

        var active = await services.Incomes.GetTemplatesAsync(includeInactive: false);

        Assert.True(active.IsSuccess, Describe(active));
        Assert.Equal(new[] { "Alfa", "Zeta" }, active.Value.Select(template => template.Concept));

        await using (var connection = new SqliteConnection($"Data Source={_options.DatabasePath};Pooling=False"))
        {
            await connection.OpenAsync();

            await using var command = connection.CreateCommand();
            command.CommandText = "UPDATE ingresos_plantillas SET activa = 0 WHERE id = $id";
            command.Parameters.AddWithValue("$id", zeta.Value.Id.ToString());
            await command.ExecuteNonQueryAsync();
        }

        var withoutInactive = await services.Incomes.GetTemplatesAsync(includeInactive: false);
        var withInactive = await services.Incomes.GetTemplatesAsync(includeInactive: true);

        Assert.True(withoutInactive.IsSuccess, Describe(withoutInactive));
        Assert.True(withInactive.IsSuccess, Describe(withInactive));
        Assert.Equal(new[] { "Alfa" }, withoutInactive.Value.Select(template => template.Concept));
        Assert.Equal(new[] { "Alfa", "Zeta" }, withInactive.Value.Select(template => template.Concept));
    }

    [Fact]
    public async Task IngresoExtraordinario_RequiereFechaReal()
    {
        var services = await CreateServicesAsync();

        var withoutDate = await services.Incomes.CreateAsync(
            new CreateIncomeRequest(
                "Venta",
                20_000,
                IncomeType.Extraordinary,
                Today,
                null,
                null,
                Guid.NewGuid()));

        Assert.True(withoutDate.IsFailure);
        Assert.Equal(ErrorCodes.FechaRequerida, Assert.Single(withoutDate.Errors).Code);

        var withDate = await services.Incomes.CreateAsync(
            new CreateIncomeRequest(
                "Venta",
                20_000,
                IncomeType.Extraordinary,
                Today,
                Today,
                null,
                Guid.NewGuid()));

        Assert.True(withDate.IsSuccess, Describe(withDate));
        Assert.Equal(IncomeStatus.Confirmed, withDate.Value.Status);
    }

    [Fact]
    public async Task IngresoPrevisto_AlConfirmarse_PasaASaldoReal()
    {
        var services = await CreateServicesAsync();

        var created = await services.Incomes.CreateAsync(
            new CreateIncomeRequest(
                "Sueldo",
                300_000,
                IncomeType.Monthly,
                new DateOnly(2026, 10, 30),
                null,
                null,
                Guid.NewGuid()));

        var before = await services.Dashboard.GetAsync(new GetDashboardRequest(Month));

        Assert.Equal(0, before.Value.IncomeCents);
        Assert.Equal(300_000, before.Value.ProjectedSavingsCents);

        var confirmed = await services.Incomes.ConfirmAsync(
            new ConfirmIncomeRequest(created.Value.Id, new DateOnly(2026, 10, 28)));

        Assert.True(confirmed.IsSuccess, Describe(confirmed));
        Assert.Equal(IncomeStatus.Confirmed, confirmed.Value.Status);

        var after = await services.Dashboard.GetAsync(new GetDashboardRequest(Month));

        Assert.Equal(300_000, after.Value.IncomeCents);
        Assert.Equal(300_000, after.Value.CurrentBalanceCents);
        Assert.Equal(300_000, after.Value.ProjectedSavingsCents);
    }

    [Fact]
    public async Task Ingreso_EdicionYAnulacion_ConservanHistorial()
    {
        var services = await CreateServicesAsync();

        var created = await services.Incomes.CreateAsync(
            new CreateIncomeRequest(
                "Freelance",
                50_000,
                IncomeType.Monthly,
                Today,
                null,
                null,
                Guid.NewGuid()));

        var updated = await services.Incomes.UpdateAsync(
            new UpdateIncomeRequest(created.Value.Id, "Freelance web", 60_000, Today));

        Assert.True(updated.IsSuccess, Describe(updated));

        var corrections = await services.Incomes.GetCorrectionsAsync(created.Value.Id);

        var correction = Assert.Single(corrections.Value);
        Assert.Equal(50_000, correction.PreviousAmountCents);
        Assert.Equal(60_000, correction.NewAmountCents);

        var cancelled = await services.Incomes.CancelAsync(
            new CancelIncomeRequest(created.Value.Id, "No se cobró"));

        Assert.True(cancelled.IsSuccess, Describe(cancelled));

        var again = await services.Incomes.CancelAsync(
            new CancelIncomeRequest(created.Value.Id, "No se cobró"));

        Assert.True(again.IsSuccess, Describe(again));
        Assert.Equal(IncomeStatus.Cancelled, again.Value.Status);
    }

    [Fact]
    public async Task SaldoInicial_SeConfiguraUnaVezYSeCorrigeConHistorial()
    {
        var services = await CreateServicesAsync();

        var missing = await services.OpeningBalance.GetAsync();
        Assert.True(missing.IsFailure);
        Assert.Equal(ErrorCodes.SaldoInicialNoConfigurado, Assert.Single(missing.Errors).Code);

        var set = await services.OpeningBalance.SetAsync(
            new SetOpeningBalanceRequest(50_000, new DateOnly(2026, 10, 1)));

        Assert.True(set.IsSuccess, Describe(set));
        Assert.Equal(50_000, set.Value.AmountCents);

        var second = await services.OpeningBalance.SetAsync(
            new SetOpeningBalanceRequest(70_000, new DateOnly(2026, 10, 1)));

        Assert.True(second.IsFailure);
        Assert.Equal(ErrorCodes.SaldoInicialYaConfigurado, Assert.Single(second.Errors).Code);

        var corrected = await services.OpeningBalance.CorrectAsync(
            new CorrectOpeningBalanceRequest(60_000, new DateOnly(2026, 10, 1)));

        Assert.True(corrected.IsSuccess, Describe(corrected));
        Assert.Equal(60_000, corrected.Value.AmountCents);

        var corrections = await services.OpeningBalance.GetCorrectionsAsync();

        var correction = Assert.Single(corrections.Value);
        Assert.Equal(50_000, correction.PreviousAmountCents);
        Assert.Equal(60_000, correction.NewAmountCents);
    }

    [Fact]
    public async Task Dashboard_IntegraSaldoIngresosGastosYCobrosPendientes()
    {
        var services = await CreateServicesAsync();

        await services.OpeningBalance.SetAsync(
            new SetOpeningBalanceRequest(50_000, new DateOnly(2026, 10, 1)));

        await services.Incomes.CreateAsync(
            new CreateIncomeRequest(
                "Sueldo",
                30_000,
                IncomeType.Monthly,
                new DateOnly(2026, 10, 1),
                new DateOnly(2026, 10, 1),
                null,
                Guid.NewGuid()));

        var plannedExpense = await services.Expenses.CreateAsync(
            new CreateExpenseRequest(
                "Compra",
                Food,
                4_000,
                new DateOnly(2026, 10, 20),
                null,
                Guid.NewGuid()));

        await services.Expenses.ConfirmAsync(
            new ConfirmExpenseRequest(plannedExpense.Value.Id, new DateOnly(2026, 10, 20)));

        await services.Incomes.CreateAsync(
            new CreateIncomeRequest(
                "Devolución",
                10_000,
                IncomeType.Monthly,
                new DateOnly(2026, 10, 30),
                null,
                null,
                Guid.NewGuid()));

        await services.Expenses.CreateAsync(
            new CreateExpenseRequest(
                "Cine",
                Transport,
                2_000,
                new DateOnly(2026, 10, 27),
                null,
                Guid.NewGuid()));

        var subscription = await services.Subscriptions.CreateAsync(
            new CreateSubscriptionRequest(
                "Netflix",
                5_000,
                AmountType.Fixed,
                BillingFrequency.Monthly,
                new DateOnly(2026, 10, 28),
                Guid.NewGuid()));

        await services.Charges.GenerateAsync(
            new GenerateChargesRequest(subscription.Value.Id, new DateOnly(2026, 10, 31)));

        var dashboard = await services.Dashboard.GetAsync(new GetDashboardRequest(Month));

        Assert.True(dashboard.IsSuccess, Describe(dashboard));

        Assert.Equal(1, dashboard.Value.ActiveSubscriptions);
        Assert.Equal(30_000, dashboard.Value.IncomeCents);
        Assert.Equal(4_000, dashboard.Value.ExpensesCents);
        Assert.Equal(0, dashboard.Value.SubscriptionPaymentsCents);
        Assert.Equal(4_000, dashboard.Value.TotalExpensesCents);
        Assert.Equal(50_000, dashboard.Value.OpeningBalanceCents);
        Assert.Equal(76_000, dashboard.Value.CurrentBalanceCents);
        Assert.Equal(79_000, dashboard.Value.ProjectedSavingsCents);
        Assert.Single(dashboard.Value.UpcomingCharges);
        Assert.Empty(dashboard.Value.OverdueCharges);
    }

    [Fact]
    public async Task Dashboard_MuestraCobrosAtrasados()
    {
        var services = await CreateServicesAsync();

        var subscription = await services.Subscriptions.CreateAsync(
            new CreateSubscriptionRequest(
                "Gimnasio",
                8_000,
                AmountType.Fixed,
                BillingFrequency.Monthly,
                new DateOnly(2026, 10, 10),
                Guid.NewGuid(),
                [new InitialPendingCharge(new DateOnly(2026, 10, 10), 8_000)]));

        Assert.True(subscription.IsSuccess, Describe(subscription));

        var dashboard = await services.Dashboard.GetAsync(new GetDashboardRequest(Month));

        var overdue = Assert.Single(dashboard.Value.OverdueCharges);
        Assert.Equal(new DateOnly(2026, 10, 10), overdue.ScheduledDate);
        Assert.Empty(dashboard.Value.UpcomingCharges);
    }

    [Fact]
    public async Task Dashboard_SinSaldoInicial_UsaCero()
    {
        var services = await CreateServicesAsync();

        var dashboard = await services.Dashboard.GetAsync(new GetDashboardRequest(Month));

        Assert.True(dashboard.IsSuccess, Describe(dashboard));
        Assert.Equal(0, dashboard.Value.OpeningBalanceCents);
        Assert.Equal(0, dashboard.Value.CurrentBalanceCents);
    }

    private async Task<SubTrackServices> CreateServicesAsync(DateOnly? today = null)
    {
        var migration = await new SqliteDatabaseMigrator(_options).MigrateAsync();

        Assert.True(migration.Success);

        return new SubTrackServices(_options, new FixedTimeProvider(today ?? Today));
    }

    private static string Describe<T>(Result<T> result) =>
        string.Join(" | ", result.Errors.Select(error => $"{error.Code}: {error.Message}"));
}
