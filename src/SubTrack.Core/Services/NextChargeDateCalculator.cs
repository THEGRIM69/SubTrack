namespace SubTrack.Core.Services;

using System;
using SubTrack.Core.Domain.Enums;

/// <summary>
/// Service that calculates the next scheduled charge date based on the previous scheduled date
/// and the billing frequency. The calculation follows the rules confirmed in the documentation:
///   • Daily – add 1 day
///   • Weekly – add 7 days
///   • Monthly – add 1 calendar month (DateOnly.AddMonths)
///   • Quarterly – add 3 calendar months
///   • Semi‑annual – add 6 calendar months
///   • Annual – add 12 calendar months
/// The method validates the inputs and throws technical exceptions for unknown frequencies
/// or dates outside the allowed <see cref="DateOnly"/> range.
/// </summary>
public static class NextChargeDateCalculator
{
    /// <summary>
    /// Calculates the next scheduled date.
    /// </summary>
    /// <param name="lastScheduledDate">The last programmed charge date.</param>
    /// <param name="frequency">The billing frequency.</param>
    /// <returns>The next scheduled charge date.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="frequency"/> is not recognised.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the supplied date is outside the <see cref="DateOnly"/> bounds.
    /// </exception>
    public static DateOnly CalculateNextDate(DateOnly lastScheduledDate, BillingFrequency frequency)
    {
        // Technical validation of the date range – DateOnly supports 0001‑01‑01 to 9999‑12‑31.
        if (lastScheduledDate < DateOnly.MinValue || lastScheduledDate > DateOnly.MaxValue)
        {
            throw new ArgumentException($"Date {lastScheduledDate} is outside the valid DateOnly range.");
        }

        return frequency switch
        {
            BillingFrequency.Daily => lastScheduledDate.AddDays(1),
            BillingFrequency.Weekly => lastScheduledDate.AddDays(7),
            BillingFrequency.Monthly => lastScheduledDate.AddMonths(1),
            BillingFrequency.Quarterly => lastScheduledDate.AddMonths(3),
            BillingFrequency.SemiAnnual => lastScheduledDate.AddMonths(6),
            BillingFrequency.Annual => lastScheduledDate.AddMonths(12),
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), $"Unsupported billing frequency: {frequency}")
        };
    }
}
