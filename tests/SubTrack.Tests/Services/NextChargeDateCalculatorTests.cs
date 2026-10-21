using System;
using SubTrack.Core.Domain.Enums;
using SubTrack.Core.Services;
using Xunit;

namespace SubTrack.Tests.Services;

public class NextChargeDateCalculatorTests
{
    [Theory]
    [InlineData("2026-01-01", BillingFrequency.Daily, "2026-01-02")]
    [InlineData("2026-01-01", BillingFrequency.Weekly, "2026-01-08")]
    [InlineData("2026-01-31", BillingFrequency.Monthly, "2026-02-28")]
    [InlineData("2026-02-28", BillingFrequency.Monthly, "2026-03-28")]
    [InlineData("2024-02-29", BillingFrequency.Monthly, "2024-03-29")]
    [InlineData("2026-01-31", BillingFrequency.Quarterly, "2026-04-30")]
    [InlineData("2026-01-31", BillingFrequency.SemiAnnual, "2026-07-31")]
    [InlineData("2026-12-31", BillingFrequency.Annual, "2027-12-31")]
    public void CalculateNextDate_ValidFrequencies_ReturnsExpected(string lastDateStr, BillingFrequency freq, string expectedStr)
    {
        var lastDate = DateOnly.Parse(lastDateStr);
        var expected = DateOnly.Parse(expectedStr);
        var result = NextChargeDateCalculator.CalculateNextDate(lastDate, freq);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void CalculateNextDate_InvalidFrequency_Throws()
    {
        // Cast an int outside the defined enum values
        var invalidFreq = (BillingFrequency)999;
        var date = DateOnly.FromDateTime(DateTime.Today);
        Assert.Throws<ArgumentOutOfRangeException>(() => NextChargeDateCalculator.CalculateNextDate(date, invalidFreq));
    }

    [Fact]
    public void CalculateNextDate_DateOutOfRange_Throws()
    {
        var maxDate = DateOnly.MaxValue; // using max date to trigger overflow on next day
        Assert.Throws<ArgumentOutOfRangeException>(() => NextChargeDateCalculator.CalculateNextDate(maxDate, BillingFrequency.Daily));
    }
}
