using SubTrack.Core.Domain.Entities;

namespace SubTrack.Core.Contracts;

/// <summary>
/// Cualquier fecha dentro del mes que se quiere consultar.
/// </summary>
public sealed record GetDashboardRequest(DateOnly Month);

/// <summary>
/// F0-11, F0-T12 — Indicadores del Dashboard ya calculados por Core.
/// </summary>
public sealed record DashboardSnapshot(
    DateOnly Month,
    int ActiveSubscriptions,
    IReadOnlyList<ScheduledCharge> UpcomingCharges,
    IReadOnlyList<ScheduledCharge> OverdueCharges,
    long SubscriptionPaymentsCents,
    long ExpensesCents,
    long TotalExpensesCents,
    long IncomeCents,
    long OpeningBalanceCents,
    long CurrentBalanceCents,
    long ProjectedSavingsCents);
