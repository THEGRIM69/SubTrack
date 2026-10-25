using System.Globalization;
using SubTrack.Core.Domain.Enums;

namespace SubTrack.WinForms.UI;

/// <summary>
/// Formato de presentación. Los importes viajan siempre en centavos.
/// </summary>
internal static class Format
{
    private static readonly CultureInfo Spanish = CultureInfo.GetCultureInfo("es-PE");

    public static string Money(long cents) => (cents / 100m).ToString("N2");

    public static string Currency(long cents) => $"S/ {Money(cents)}";

    public static string Month(DateOnly value) => Capitalize(value.ToString("MMMM yyyy", Spanish));

    public static string MonthShort(DateOnly value) => Capitalize(value.ToString("MMM", Spanish));

    public static long Cents(decimal amount) =>
        (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);

    public static string Date(DateOnly? date) => date?.ToString("dd/MM/yyyy") ?? "—";

    public static string Date(DateOnly date) => date.ToString("dd/MM/yyyy");

    public static string Capitalize(string value) =>
        value.Length == 0 ? value : char.ToUpper(value[0], Spanish) + value[1..];

    public static string Stamp(DateTimeOffset value) =>
        value.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

    public static string AmountType(AmountType value) => value switch
    {
        SubTrack.Core.Domain.Enums.AmountType.Fixed => "Fija",
        SubTrack.Core.Domain.Enums.AmountType.Variable => "Variable",
        _ => value.ToString()
    };

    public static string Frequency(BillingFrequency value) => value switch
    {
        BillingFrequency.Daily => "Diaria",
        BillingFrequency.Weekly => "Semanal",
        BillingFrequency.Monthly => "Mensual",
        BillingFrequency.Quarterly => "Trimestral",
        BillingFrequency.SemiAnnual => "Semestral",
        BillingFrequency.Annual => "Anual",
        _ => value.ToString()
    };

    public static string ExpenseStatus(ExpenseStatus value) => value switch
    {
        SubTrack.Core.Domain.Enums.ExpenseStatus.Planned => "Previsto",
        SubTrack.Core.Domain.Enums.ExpenseStatus.Confirmed => "Confirmado",
        SubTrack.Core.Domain.Enums.ExpenseStatus.Cancelled => "Anulado",
        _ => value.ToString()
    };

    public static string IncomeStatus(IncomeStatus value) => value switch
    {
        SubTrack.Core.Domain.Enums.IncomeStatus.Planned => "Previsto",
        SubTrack.Core.Domain.Enums.IncomeStatus.Confirmed => "Confirmado",
        SubTrack.Core.Domain.Enums.IncomeStatus.Cancelled => "Anulado",
        _ => value.ToString()
    };

    public static string IncomeType(IncomeType value) => value switch
    {
        SubTrack.Core.Domain.Enums.IncomeType.Monthly => "Mensual",
        SubTrack.Core.Domain.Enums.IncomeType.Biweekly => "Quincenal",
        SubTrack.Core.Domain.Enums.IncomeType.Extraordinary => "Extraordinario",
        _ => value.ToString()
    };

    public static string ChargeStatus(ScheduledChargeStatus value) => value switch
    {
        ScheduledChargeStatus.Pending => "Pendiente",
        ScheduledChargeStatus.Paid => "Pagado",
        ScheduledChargeStatus.Cancelled => "Anulado",
        _ => value.ToString()
    };

    public static string PaymentStatus(PaymentStatus value) => value switch
    {
        SubTrack.Core.Domain.Enums.PaymentStatus.Active => "Vigente",
        SubTrack.Core.Domain.Enums.PaymentStatus.Cancelled => "Anulado",
        _ => value.ToString()
    };
}
