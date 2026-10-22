using System;
using SubTrack.Core.Domain.Enums;

namespace SubTrack.Core.Services;

/// <summary>
/// Provides validation to verify whether a candidate date corresponds to the expected
/// next scheduled charge date based on the previous scheduled date and the billing frequency.
/// This service re‑uses <see cref="NextChargeDateCalculator"/> to avoid duplicating calculation rules.
/// </summary>
public static class RecurrenceScheduleValidator
{
    /// <summary>
    /// Determines if <paramref name="candidateDate"/> is exactly the next date that would be calculated
    /// by <see cref="NextChargeDateCalculator.CalculateNextDate"/> for the given <paramref name="previousScheduledDate"/> and <paramref name="frequency"/>.
    /// </summary>
    /// <param name="previousScheduledDate">The last scheduled charge date.</param>
    /// <param name="candidateDate">The date to validate.</param>
    /// <param name="frequency">The billing frequency.</param>
    /// <returns>True when the candidate matches the expected next date; otherwise false.</returns>
    /// <exception cref="ArgumentException">Propagates the same exception as <see cref="NextChargeDateCalculator"/> when the previous date is out of the <see cref="DateOnly"/> bounds.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Propagates when the frequency is not recognised.</exception>
    public static bool IsExpectedNextDate(
        DateOnly previousScheduledDate,
        DateOnly candidateDate,
        BillingFrequency frequency)
    {
        // Reuse the existing calculation logic – any validation of the previous date or frequency
        // is performed inside the calculator and will be propagated unchanged.
        var expectedNext = NextChargeDateCalculator.CalculateNextDate(previousScheduledDate, frequency);
        return expectedNext.Equals(candidateDate);
    }
}
