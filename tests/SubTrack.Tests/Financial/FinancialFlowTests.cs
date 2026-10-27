using Microsoft.Data.Sqlite;
using SubTrack.Core.Contracts;
using SubTrack.Core.Domain.Enums;
using SubTrack.Core.Results;
using SubTrack.Infrastructure;
using SubTrack.Infrastructure.Persistence;

namespace SubTrack.Tests.Financial;

/// <summary>
/// F3, F4 y F5 — Flujo financiero completo contra una base SQLite real.
/// Cada prueba usa su propia base y un reloj fijo para no depender de la fecha del equipo.
/// </summary>
public sealed class FinancialFlowTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 10, 25);
    private static readonly DateOnly Start = new(2026, 10, 28);

    private readonly string _tempDirectory;
    private readonly SubTrackDatabaseOptions _options;

    public FinancialFlowTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "SubTrack.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);

        _options = new SubTrackDatabaseOptions
        {
            DatabasePath = Path.Combine(_tempDirectory, "finanzas.db"),
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
            // El sistema puede tardar en liberar el archivo; la carpeta es temporal.
        }
    }

    [Fact]
    public async Task CrearSuscripcion_RegistraVersionInicialYProximoCobro()
    {
        var services = await CreateServicesAsync();

        var result = await services.Subscriptions.CreateAsync(NewSubscription());

        Assert.True(result.IsSuccess, Describe(result));
        Assert.Equal(Start, result.Value.NextChargeDate);
        Assert.Equal(28, result.Value.AnchorDay);
        Assert.True(result.Value.IsActive);

        var versions = await services.Subscriptions.GetVersionsAsync(result.Value.Id);

        Assert.True(versions.IsSuccess, Describe(versions));
        var version = Assert.Single(versions.Value);
        Assert.Equal(1, version.VersionNumber);
        Assert.Equal(Start, version.EffectiveFrom);
        Assert.Equal(10_000, version.AmountCents);
        Assert.Equal(28, version.AnchorDay);
    }

    [Fact]
    public async Task CrearSuscripcion_ConMismaOperacion_NoDuplica()
    {
        var services = await CreateServicesAsync();
        var operationId = Guid.NewGuid();

        var first = await services.Subscriptions.CreateAsync(NewSubscription(operationId: operationId));
        var second = await services.Subscriptions.CreateAsync(NewSubscription(operationId: operationId));

        Assert.True(first.IsSuccess, Describe(first));
        Assert.True(second.IsSuccess, Describe(second));
        Assert.Equal(first.Value.Id, second.Value.Id);

        var all = await services.Subscriptions.GetAllAsync();

        Assert.Single(all.Value);
    }

    [Fact]
    public async Task CrearSuscripcion_SinOperacion_LanzaErrorDeValidacion()
    {
        var services = await CreateServicesAsync();

        var result = await services.Subscriptions.CreateAsync(
            NewSubscription() with { OperationId = Guid.Empty });

        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors, error => error.Code == ErrorCodes.OperacionInvalida);
    }

    [Fact]
    public async Task GenerarCobros_CreaSoloLosQueFaltan()
    {
        var services = await CreateServicesAsync();
        var subscription = await CreateSubscriptionAsync(services);

        var generated = await services.Charges.GenerateAsync(
            new GenerateChargesRequest(subscription.Id, new DateOnly(2027, 1, 31)));

        Assert.True(generated.IsSuccess, Describe(generated));
        Assert.Equal(
            new[]
            {
                new DateOnly(2026, 10, 28),
                new DateOnly(2026, 11, 28),
                new DateOnly(2026, 12, 28),
                new DateOnly(2027, 1, 28)
            },
            generated.Value.Select(charge => charge.ScheduledDate));

        var again = await services.Charges.GenerateAsync(
            new GenerateChargesRequest(subscription.Id, new DateOnly(2027, 1, 31)));

        Assert.True(again.IsSuccess, Describe(again));
        Assert.Empty(again.Value);
    }

    [Fact]
    public async Task GenerarCobros_MasDeDoceMeses_SeRechaza()
    {
        var services = await CreateServicesAsync();
        var subscription = await CreateSubscriptionAsync(services);

        var result = await services.Charges.GenerateAsync(
            new GenerateChargesRequest(subscription.Id, Today.AddMonths(13)));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.RangoExcedido, Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task RegistrarPago_EmiteComprobanteYAvanzaElProximoCobro()
    {
        var services = await CreateServicesAsync();
        var subscription = await CreateSubscriptionAsync(services);
        await GenerateAsync(services, subscription.Id, new DateOnly(2026, 12, 31));

        var charge = await GetSingleChargeAsync(services, new DateOnly(2026, 10, 28));

        var result = await services.Payments.RegisterAsync(
            new RegisterPaymentRequest(charge.Id, Today, Guid.NewGuid()));

        Assert.True(result.IsSuccess, Describe(result));
        Assert.Equal("CMP-2026-000001", result.Value.VoucherNumber);
        Assert.Equal(10_000, result.Value.AmountCents);
        Assert.Equal(PaymentStatus.Active, result.Value.Status);

        var after = await services.Charges.GetByRangeAsync(
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 10, 31));

        Assert.Equal(ScheduledChargeStatus.Paid, Assert.Single(after.Value).Status);

        var updated = await services.Subscriptions.GetByIdAsync(subscription.Id);

        Assert.Equal(new DateOnly(2026, 11, 28), updated.Value.NextChargeDate);
    }

    [Fact]
    public async Task RegistrarPago_EnElMismoCobro_SeRechaza()
    {
        var services = await CreateServicesAsync();
        var subscription = await CreateSubscriptionAsync(services);
        await GenerateAsync(services, subscription.Id, new DateOnly(2026, 10, 31));

        var charge = await GetSingleChargeAsync(services, new DateOnly(2026, 10, 28));

        var first = await services.Payments.RegisterAsync(
            new RegisterPaymentRequest(charge.Id, Today, Guid.NewGuid()));

        var second = await services.Payments.RegisterAsync(
            new RegisterPaymentRequest(charge.Id, Today, Guid.NewGuid()));

        Assert.True(first.IsSuccess, Describe(first));
        Assert.True(second.IsFailure);
        Assert.Equal(ErrorCodes.CobroYaPagado, Assert.Single(second.Errors).Code);
    }

    [Fact]
    public async Task RegistrarPago_ConMismaOperacion_DevuelveElMismoPago()
    {
        var services = await CreateServicesAsync();
        var subscription = await CreateSubscriptionAsync(services);
        await GenerateAsync(services, subscription.Id, new DateOnly(2026, 12, 31));

        var charge = await GetSingleChargeAsync(services, new DateOnly(2026, 10, 28));
        var operationId = Guid.NewGuid();

        var first = await services.Payments.RegisterAsync(
            new RegisterPaymentRequest(charge.Id, Today, operationId));

        var retry = await services.Payments.RegisterAsync(
            new RegisterPaymentRequest(charge.Id, Today, operationId));

        Assert.True(first.IsSuccess, Describe(first));
        Assert.True(retry.IsSuccess, Describe(retry));
        Assert.Equal(first.Value.Id, retry.Value.Id);
        Assert.Equal("CMP-2026-000001", retry.Value.VoucherNumber);

        var next = await services.Payments.RegisterAsync(
            new RegisterPaymentRequest(
                (await GetSingleChargeAsync(services, new DateOnly(2026, 11, 28))).Id,
                Today,
                Guid.NewGuid()));

        Assert.Equal("CMP-2026-000002", next.Value.VoucherNumber);
    }

    [Fact]
    public async Task AnularPago_DevuelveElCobroAPendienteYConservaElComprobante()
    {
        var services = await CreateServicesAsync();
        var subscription = await CreateSubscriptionAsync(services);
        await GenerateAsync(services, subscription.Id, new DateOnly(2026, 12, 31));

        var charge = await GetSingleChargeAsync(services, new DateOnly(2026, 10, 28));

        var payment = await services.Payments.RegisterAsync(
            new RegisterPaymentRequest(charge.Id, Today, Guid.NewGuid()));

        var cancelled = await services.Payments.CancelAsync(
            new CancelPaymentRequest(payment.Value.Id, "Se registró por error"));

        Assert.True(cancelled.IsSuccess, Describe(cancelled));
        Assert.Equal(PaymentStatus.Cancelled, cancelled.Value.Status);
        Assert.Equal("CMP-2026-000001", cancelled.Value.VoucherNumber);
        Assert.Equal("Se registró por error", cancelled.Value.CancellationReason);

        var pending = await services.Charges.GetPendingAsync();
        var back = Assert.Single(pending.Value, item => item.Id == charge.Id);

        Assert.Equal(ScheduledChargeStatus.Pending, back.Status);

        var updated = await services.Subscriptions.GetByIdAsync(subscription.Id);
        Assert.Equal(new DateOnly(2026, 10, 28), updated.Value.NextChargeDate);

        var rePayment = await services.Payments.RegisterAsync(
            new RegisterPaymentRequest(charge.Id, Today, Guid.NewGuid()));

        Assert.True(rePayment.IsSuccess, Describe(rePayment));
        Assert.Equal("CMP-2026-000002", rePayment.Value.VoucherNumber);
    }

    [Fact]
    public async Task AnularPago_SinMotivo_SeRechaza()
    {
        var services = await CreateServicesAsync();
        var subscription = await CreateSubscriptionAsync(services);
        await GenerateAsync(services, subscription.Id, new DateOnly(2026, 10, 31));

        var charge = await GetSingleChargeAsync(services, new DateOnly(2026, 10, 28));
        var payment = await services.Payments.RegisterAsync(
            new RegisterPaymentRequest(charge.Id, Today, Guid.NewGuid()));

        var result = await services.Payments.CancelAsync(
            new CancelPaymentRequest(payment.Value.Id, "   "));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.MotivoRequerido, Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task RegistrarPago_ConFechaFutura_SeRechaza()
    {
        var services = await CreateServicesAsync();
        var subscription = await CreateSubscriptionAsync(services);
        await GenerateAsync(services, subscription.Id, new DateOnly(2026, 10, 31));

        var charge = await GetSingleChargeAsync(services, new DateOnly(2026, 10, 28));

        var result = await services.Payments.RegisterAsync(
            new RegisterPaymentRequest(charge.Id, Today.AddDays(1), Guid.NewGuid()));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.FechaFuturaNoPermitida, Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task Comprobante_UsaElAnioDeLaFechaDePago()
    {
        var services = await CreateServicesAsync(new DateOnly(2027, 1, 10));
        var subscription = await CreateSubscriptionAsync(
            services,
            new CreateSubscriptionRequest(
                "Seguro",
                25_000,
                AmountType.Fixed,
                BillingFrequency.Monthly,
                new DateOnly(2026, 12, 28),
                Guid.NewGuid(),
                [new InitialPendingCharge(new DateOnly(2026, 12, 28), 25_000)]));

        var charge = await GetSingleChargeAsync(
            services,
            new DateOnly(2026, 12, 28),
            subscription.Id);

        var result = await services.Payments.RegisterAsync(
            new RegisterPaymentRequest(charge.Id, new DateOnly(2027, 1, 5), Guid.NewGuid()));

        Assert.True(result.IsSuccess, Describe(result));
        Assert.Equal("CMP-2027-000001", result.Value.VoucherNumber);
    }

    [Fact]
    public async Task AjustarImporte_SoloSePermiteEnSuscripcionesVariables()
    {
        var services = await CreateServicesAsync();

        var fixedSubscription = await CreateSubscriptionAsync(services);
        await GenerateAsync(services, fixedSubscription.Id, new DateOnly(2026, 10, 31));
        var fixedCharge = await GetSingleChargeAsync(
            services,
            new DateOnly(2026, 10, 28),
            fixedSubscription.Id);

        var rejected = await services.Charges.AdjustAmountAsync(
            new AdjustChargeAmountRequest(fixedCharge.Id, 12_000));

        Assert.True(rejected.IsFailure);
        Assert.Equal(ErrorCodes.AjusteNoPermitido, Assert.Single(rejected.Errors).Code);

        var variableSubscription = await CreateSubscriptionAsync(
            services,
            new CreateSubscriptionRequest(
                "Luz",
                8_000,
                AmountType.Variable,
                BillingFrequency.Monthly,
                Start,
                Guid.NewGuid()));

        await GenerateAsync(services, variableSubscription.Id, new DateOnly(2026, 10, 31));
        var variableCharge = await GetSingleChargeAsync(
            services,
            new DateOnly(2026, 10, 28),
            variableSubscription.Id);

        var adjusted = await services.Charges.AdjustAmountAsync(
            new AdjustChargeAmountRequest(variableCharge.Id, 12_500));

        Assert.True(adjusted.IsSuccess, Describe(adjusted));
        Assert.Equal(12_500, adjusted.Value.AmountCents);

        var payment = await services.Payments.RegisterAsync(
            new RegisterPaymentRequest(variableCharge.Id, Today, Guid.NewGuid()));

        Assert.Equal(12_500, payment.Value.AmountCents);
    }

    [Fact]
    public async Task ModificarSuscripcion_CreaVersionYRespetaElHistorial()
    {
        var services = await CreateServicesAsync();
        var subscription = await CreateSubscriptionAsync(services);
        await GenerateAsync(services, subscription.Id, new DateOnly(2027, 1, 31));

        var paidCharge = await GetSingleChargeAsync(services, new DateOnly(2026, 10, 28));
        await services.Payments.RegisterAsync(
            new RegisterPaymentRequest(paidCharge.Id, Today, Guid.NewGuid()));

        var updated = await services.Subscriptions.UpdateAsync(
            new UpdateSubscriptionRequest(
                subscription.Id,
                "Netflix Premium",
                14_000,
                AmountType.Fixed,
                BillingFrequency.Monthly,
                new DateOnly(2026, 11, 28)));

        Assert.True(updated.IsSuccess, Describe(updated));
        Assert.Equal(14_000, updated.Value.AmountCents);
        Assert.Equal(new DateOnly(2026, 11, 28), updated.Value.NextChargeDate);

        var charges = await services.Charges.GetByRangeAsync(
            new DateOnly(2026, 10, 1),
            new DateOnly(2027, 1, 31));

        var october = Assert.Single(charges.Value, charge => charge.ScheduledDate == new DateOnly(2026, 10, 28));
        Assert.Equal(10_000, october.AmountCents);
        Assert.Equal(ScheduledChargeStatus.Paid, october.Status);

        var november = Assert.Single(charges.Value, charge => charge.ScheduledDate == new DateOnly(2026, 11, 28));
        Assert.Equal(14_000, november.AmountCents);
        Assert.Equal(ScheduledChargeStatus.Pending, november.Status);

        var versions = await services.Subscriptions.GetVersionsAsync(subscription.Id);

        Assert.Equal(2, versions.Value.Count);
        Assert.Equal(2, versions.Value[1].VersionNumber);
        Assert.Equal(14_000, versions.Value[1].AmountCents);
    }

    [Fact]
    public async Task ModificarSuscripcion_ConFechaEfectivaDistinta_ConservaAnchorDayYVencimientosF0T31()
    {
        var services = await CreateServicesAsync(new DateOnly(2027, 1, 31));
        var subscription = await CreateSubscriptionAsync(
            services,
            new CreateSubscriptionRequest(
                "Suscripcion 31",
                10_000,
                AmountType.Fixed,
                BillingFrequency.Monthly,
                new DateOnly(2027, 1, 31),
                Guid.NewGuid()));

        Assert.Equal(31, subscription.AnchorDay);

        // Generar cobros hasta junio 2027
        await GenerateAsync(services, subscription.Id, new DateOnly(2027, 6, 30));

        // Registrar pago del cobro inicial de enero
        var firstCharge = await GetSingleChargeAsync(services, new DateOnly(2027, 1, 31), subscription.Id);
        var paymentResult = await services.Payments.RegisterAsync(
            new RegisterPaymentRequest(firstCharge.Id, new DateOnly(2027, 1, 31), Guid.NewGuid()));
        Assert.True(paymentResult.IsSuccess, Describe(paymentResult));

        // Modificar importe con entrada en vigor el 15 de abril de 2027
        var updated = await services.Subscriptions.UpdateAsync(
            new UpdateSubscriptionRequest(
                subscription.Id,
                "Suscripcion 31 Actualizada",
                15_000,
                AmountType.Fixed,
                BillingFrequency.Monthly,
                new DateOnly(2027, 4, 15)));

        Assert.True(updated.IsSuccess, Describe(updated));
        Assert.Equal(31, updated.Value.AnchorDay);
        Assert.Equal(15_000, updated.Value.AmountCents);

        var versions = await services.Subscriptions.GetVersionsAsync(subscription.Id);
        Assert.True(versions.IsSuccess, Describe(versions));
        Assert.Equal(2, versions.Value.Count);
        Assert.Equal(31, versions.Value[0].AnchorDay);
        Assert.Equal(31, versions.Value[1].AnchorDay);
        Assert.Equal(new DateOnly(2027, 4, 15), versions.Value[1].EffectiveFrom);

        var chargesResult = await services.Charges.GetByRangeAsync(
            new DateOnly(2027, 1, 1),
            new DateOnly(2027, 6, 30));

        Assert.True(chargesResult.IsSuccess, Describe(chargesResult));
        var charges = chargesResult.Value
            .Where(c => c.SubscriptionId == subscription.Id)
            .OrderBy(c => c.ScheduledDate)
            .ToList();

        // No debe existir cobro administrativo el 15 de abril
        Assert.DoesNotContain(charges, c => c.ScheduledDate == new DateOnly(2027, 4, 15));

        // Deben existir exactamente 6 cobros con las fechas F0-T31
        var expectedDates = new[]
        {
            new DateOnly(2027, 1, 31),
            new DateOnly(2027, 2, 28),
            new DateOnly(2027, 3, 31),
            new DateOnly(2027, 4, 30),
            new DateOnly(2027, 5, 31),
            new DateOnly(2027, 6, 30)
        };

        Assert.Equal(expectedDates, charges.Select(c => c.ScheduledDate).ToArray());

        // Cobro historico pagado intacto
        Assert.Equal(ScheduledChargeStatus.Paid, charges[0].Status);
        Assert.Equal(10_000, charges[0].AmountCents);

        // Cobros anteriores al 15 de abril con importe original
        Assert.Equal(ScheduledChargeStatus.Pending, charges[1].Status);
        Assert.Equal(10_000, charges[1].AmountCents);

        Assert.Equal(ScheduledChargeStatus.Pending, charges[2].Status);
        Assert.Equal(10_000, charges[2].AmountCents);

        // Cobros posteriores al 15 de abril con nuevo importe
        Assert.Equal(ScheduledChargeStatus.Pending, charges[3].Status);
        Assert.Equal(15_000, charges[3].AmountCents);

        Assert.Equal(ScheduledChargeStatus.Pending, charges[4].Status);
        Assert.Equal(15_000, charges[4].AmountCents);

        Assert.Equal(ScheduledChargeStatus.Pending, charges[5].Status);
        Assert.Equal(15_000, charges[5].AmountCents);
    }

    [Fact]
    public async Task ModificarSuscripcion_ConFechaAnteriorAUltimaVersion_SeRechaza()
    {
        var services = await CreateServicesAsync();
        var subscription = await CreateSubscriptionAsync(services);

        var result = await services.Subscriptions.UpdateAsync(
            new UpdateSubscriptionRequest(
                subscription.Id,
                "Netflix",
                12_000,
                AmountType.Fixed,
                BillingFrequency.Monthly,
                Start));

        Assert.True(result.IsFailure);
        Assert.Equal(ErrorCodes.FechaEfectivaAnterior, Assert.Single(result.Errors).Code);
    }

    [Fact]
    public async Task DesactivarSuscripcion_ConservaCobrosYBloqueaGeneracion()
    {
        var services = await CreateServicesAsync();
        var subscription = await CreateSubscriptionAsync(services);
        await GenerateAsync(services, subscription.Id, new DateOnly(2026, 12, 31));

        var result = await services.Subscriptions.DeactivateAsync(
            new DeactivateSubscriptionRequest(subscription.Id, new DateOnly(2026, 11, 1)));

        Assert.True(result.IsSuccess, Describe(result));
        Assert.False(result.Value.IsActive);
        Assert.Equal(new DateOnly(2026, 11, 1), result.Value.DeactivatedOn);
        Assert.Equal(new DateOnly(2026, 10, 28), result.Value.NextChargeDate);

        var generate = await services.Charges.GenerateAsync(
            new GenerateChargesRequest(subscription.Id, new DateOnly(2026, 12, 31)));

        Assert.True(generate.IsFailure);
        Assert.Equal(ErrorCodes.SuscripcionInactiva, Assert.Single(generate.Errors).Code);

        var charges = await services.Charges.GetByRangeAsync(
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 12, 31));

        Assert.Equal(3, charges.Value.Count);
    }

    [Fact]
    public async Task HistorialDePagos_SePaginaYOrdenaPorFechaDescendente()
    {
        var services = await CreateServicesAsync(new DateOnly(2026, 12, 31));
        await CreateSubscriptionAsync(
            services,
            new CreateSubscriptionRequest(
                "Netflix",
                10_000,
                AmountType.Fixed,
                BillingFrequency.Monthly,
                Start,
                Guid.NewGuid(),
                [
                    new InitialPendingCharge(new DateOnly(2026, 10, 28), 10_000),
                    new InitialPendingCharge(new DateOnly(2026, 11, 28), 10_000),
                    new InitialPendingCharge(new DateOnly(2026, 12, 28), 10_000)
                ]));

        var charges = await services.Charges.GetByRangeAsync(
            new DateOnly(2026, 10, 1),
            new DateOnly(2026, 12, 31));

        await services.Payments.RegisterAsync(
            new RegisterPaymentRequest(charges.Value[0].Id, new DateOnly(2026, 10, 28), Guid.NewGuid()));

        await services.Payments.RegisterAsync(
            new RegisterPaymentRequest(charges.Value[1].Id, new DateOnly(2026, 11, 28), Guid.NewGuid()));

        await services.Payments.RegisterAsync(
            new RegisterPaymentRequest(charges.Value[2].Id, new DateOnly(2026, 12, 28), Guid.NewGuid()));

        var page = await services.Payments.GetHistoryAsync(new PageRequest(Page: 1, PageSize: 2));

        Assert.True(page.IsSuccess, Describe(page));
        Assert.Equal(3, page.Value.TotalCount);
        Assert.Equal(2, page.Value.Items.Count);
        Assert.True(page.Value.HasNextPage);
        Assert.Equal(new DateOnly(2026, 12, 28), page.Value.Items[0].PaymentDate);
        Assert.Equal(new DateOnly(2026, 11, 28), page.Value.Items[1].PaymentDate);
    }

    private async Task<SubTrackServices> CreateServicesAsync(DateOnly? today = null)
    {
        var migration = await new SqliteDatabaseMigrator(_options).MigrateAsync();

        Assert.True(migration.Success);

        return new SubTrackServices(_options, new FixedTimeProvider(today ?? Today));
    }

    private static CreateSubscriptionRequest NewSubscription(Guid? operationId = null) =>
        new(
            "Netflix",
            10_000,
            AmountType.Fixed,
            BillingFrequency.Monthly,
            Start,
            operationId ?? Guid.NewGuid());

    private static async Task<SubTrack.Core.Domain.Entities.Subscription> CreateSubscriptionAsync(
        SubTrackServices services,
        CreateSubscriptionRequest? request = null)
    {
        var result = await services.Subscriptions.CreateAsync(request ?? NewSubscription());

        Assert.True(result.IsSuccess, Describe(result));

        return result.Value;
    }

    private static async Task GenerateAsync(
        SubTrackServices services,
        Guid subscriptionId,
        DateOnly until)
    {
        var result = await services.Charges.GenerateAsync(
            new GenerateChargesRequest(subscriptionId, until));

        Assert.True(result.IsSuccess, Describe(result));
    }

    private static async Task<SubTrack.Core.Domain.Entities.ScheduledCharge> GetSingleChargeAsync(
        SubTrackServices services,
        DateOnly date,
        Guid? subscriptionId = null)
    {
        var charges = await services.Charges.GetByRangeAsync(date, date);

        Assert.True(charges.IsSuccess, Describe(charges));

        var matches = subscriptionId is null
            ? charges.Value
            : charges.Value.Where(charge => charge.SubscriptionId == subscriptionId).ToList();

        return Assert.Single(matches);
    }

    private static string Describe<T>(Result<T> result) =>
        string.Join(" | ", result.Errors.Select(error => $"{error.Code}: {error.Message}"));
}

/// <summary>Reloj fijo en UTC para que las reglas de «hoy» sean deterministas.</summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _utcNow;

    public FixedTimeProvider(DateOnly today)
    {
        _utcNow = new DateTimeOffset(today.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero);
    }

    public override DateTimeOffset GetUtcNow() => _utcNow;

    public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
}
