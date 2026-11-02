namespace SubTrack.Core.Domain.Enums;

/// <summary>
/// Frecuencia de cobro recurrente.
/// Provisional hasta que los contratos de integración sean aprobados.
/// </summary>
public enum BillingFrequency
{
    Daily = 1,
    Weekly = 2,
    Monthly = 3,
    Quarterly = 4,
    SemiAnnual = 5,
    Annual = 6
}
