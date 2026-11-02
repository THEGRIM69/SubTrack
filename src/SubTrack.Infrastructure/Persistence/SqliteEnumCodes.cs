using SubTrack.Core.Domain.Enums;

namespace SubTrack.Infrastructure.Persistence;

/// <summary>
/// Correspondencia entre las enumeraciones de Core y los códigos estables de SQLite.
/// Es el único punto donde se traducen; el resto del código usa los tipos de Core.
/// </summary>
internal static class SqliteEnumCodes
{
    public static string ToCode(AmountType value) => value switch
    {
        AmountType.Fixed => "FIJO",
        AmountType.Variable => "VARIABLE",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Tipo de importe no soportado.")
    };

    public static AmountType ParseAmountType(string code) => code switch
    {
        "FIJO" => AmountType.Fixed,
        "VARIABLE" => AmountType.Variable,
        _ => throw new InvalidOperationException($"Código de tipo de importe desconocido: '{code}'.")
    };

    public static string ToCode(BillingFrequency value) => value switch
    {
        BillingFrequency.Daily => "DIARIA",
        BillingFrequency.Weekly => "SEMANAL",
        BillingFrequency.Monthly => "MENSUAL",
        BillingFrequency.Quarterly => "TRIMESTRAL",
        BillingFrequency.SemiAnnual => "SEMESTRAL",
        BillingFrequency.Annual => "ANUAL",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Frecuencia no soportada.")
    };

    public static BillingFrequency ParseFrequency(string code) => code switch
    {
        "DIARIA" => BillingFrequency.Daily,
        "SEMANAL" => BillingFrequency.Weekly,
        "MENSUAL" => BillingFrequency.Monthly,
        "TRIMESTRAL" => BillingFrequency.Quarterly,
        "SEMESTRAL" => BillingFrequency.SemiAnnual,
        "ANUAL" => BillingFrequency.Annual,
        _ => throw new InvalidOperationException($"Código de frecuencia desconocido: '{code}'.")
    };

    public static string ToCode(ScheduledChargeStatus value) => value switch
    {
        ScheduledChargeStatus.Pending => "PENDIENTE",
        ScheduledChargeStatus.Paid => "PAGADO",
        ScheduledChargeStatus.Cancelled => "CANCELADO",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Estado de cobro no soportado.")
    };

    public static ScheduledChargeStatus ParseChargeStatus(string code) => code switch
    {
        "PENDIENTE" => ScheduledChargeStatus.Pending,
        "PAGADO" => ScheduledChargeStatus.Paid,
        "CANCELADO" => ScheduledChargeStatus.Cancelled,
        _ => throw new InvalidOperationException($"Estado de cobro desconocido: '{code}'.")
    };

    public static string ToCode(PaymentStatus value) => value switch
    {
        PaymentStatus.Active => "VIGENTE",
        PaymentStatus.Cancelled => "ANULADO",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Estado de pago no soportado.")
    };

    public static PaymentStatus ParsePaymentStatus(string code) => code switch
    {
        "VIGENTE" => PaymentStatus.Active,
        "ANULADO" => PaymentStatus.Cancelled,
        _ => throw new InvalidOperationException($"Estado de pago desconocido: '{code}'.")
    };

    public static string ToCode(ExpenseStatus value) => value switch
    {
        ExpenseStatus.Planned => "PLANIFICADO",
        ExpenseStatus.Confirmed => "CONFIRMADO",
        ExpenseStatus.Cancelled => "ANULADO",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Estado de gasto no soportado.")
    };

    public static ExpenseStatus ParseExpenseStatus(string code) => code switch
    {
        "PLANIFICADO" => ExpenseStatus.Planned,
        "CONFIRMADO" => ExpenseStatus.Confirmed,
        "ANULADO" => ExpenseStatus.Cancelled,
        _ => throw new InvalidOperationException($"Estado de gasto desconocido: '{code}'.")
    };

    public static string ToCode(IncomeStatus value) => value switch
    {
        IncomeStatus.Planned => "PREVISTO",
        IncomeStatus.Confirmed => "CONFIRMADO",
        IncomeStatus.Cancelled => "ANULADO",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Estado de ingreso no soportado.")
    };

    public static IncomeStatus ParseIncomeStatus(string code) => code switch
    {
        "PREVISTO" => IncomeStatus.Planned,
        "CONFIRMADO" => IncomeStatus.Confirmed,
        "ANULADO" => IncomeStatus.Cancelled,
        _ => throw new InvalidOperationException($"Estado de ingreso desconocido: '{code}'.")
    };

    public static string ToCode(IncomeType value) => value switch
    {
        IncomeType.Monthly => "MENSUAL",
        IncomeType.Biweekly => "QUINCENAL",
        IncomeType.Extraordinary => "EXTRAORDINARIO",
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Tipo de ingreso no soportado.")
    };

    public static IncomeType ParseIncomeType(string code) => code switch
    {
        "MENSUAL" => IncomeType.Monthly,
        "QUINCENAL" => IncomeType.Biweekly,
        "EXTRAORDINARIO" => IncomeType.Extraordinary,
        _ => throw new InvalidOperationException($"Tipo de ingreso desconocido: '{code}'.")
    };
}
