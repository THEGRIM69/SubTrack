using System;
using SubTrack.Core.Domain.Enums;
using SubTrack.Core.Services;
using Xunit;

namespace SubTrack.Tests.Services
{
    public class RecurrenceScheduleValidatorTests
    {
        // Helper to create a DateOnly from year, month, day
        private static DateOnly D(int year, int month, int day) => new DateOnly(year, month, day);

        [Theory]
        [InlineData(2023, 1, 1, 2023, 1, 2, BillingFrequency.Daily)] // daily next day
        [InlineData(2023, 1, 1, 2023, 1, 8, BillingFrequency.Weekly)] // weekly +7 days
        [InlineData(2023, 1, 31, 2023, 2, 28, BillingFrequency.Monthly)] // month end adjustment (non‑leap year)
        [InlineData(2024, 1, 31, 2024, 2, 29, BillingFrequency.Monthly)] // month end leap year
        [InlineData(2023, 2, 28, 2023, 3, 28, BillingFrequency.Monthly)] // regular month progression
        [InlineData(2023, 3, 31, 2023, 4, 30, BillingFrequency.Monthly)] // end‑of‑April
        [InlineData(2023, 1, 31, 2023, 4, 30, BillingFrequency.Quarterly)] // quarterly
        [InlineData(2023, 1, 31, 2023, 7, 31, BillingFrequency.SemiAnnual)] // semi‑annual
        [InlineData(2023, 1, 31, 2024, 1, 31, BillingFrequency.Annual)] // annual
        public void IsExpectedNextDate_ReturnsTrue_ForValidNextDates(int yearPrev, int monthPrev, int dayPrev,
                                                                   int yearExp, int monthExp, int dayExp,
                                                                   BillingFrequency freq)
        {
            var previous = D(yearPrev, monthPrev, dayPrev);
            var expected = D(yearExp, monthExp, dayExp);
            bool result = RecurrenceScheduleValidator.IsExpectedNextDate(previous, expected, freq);
            Assert.True(result);
        }

        [Theory]
        [InlineData(2023, 1, 1, 2023, 1, 3, BillingFrequency.Daily)] // off by one day
        [InlineData(2023, 1, 1, 2023, 1, 9, BillingFrequency.Weekly)] // off by one day
        [InlineData(2023, 1, 31, 2023, 2, 27, BillingFrequency.Monthly)] // wrong month‑end
        public void IsExpectedNextDate_ReturnsFalse_ForInvalidCandidate(int yPrev, int mPrev, int dPrev,
                                                                      int yCand, int mCand, int dCand,
                                                                      BillingFrequency freq)
        {
            var previous = D(yPrev, mPrev, dPrev);
            var candidate = D(yCand, mCand, dCand);
            bool result = RecurrenceScheduleValidator.IsExpectedNextDate(previous, candidate, freq);
            Assert.False(result);
        }

        [Fact]
        public void IsExpectedNextDate_ThrowsWhenFrequencyUnknown()
        {
            // Cast an invalid integer to the enum to simulate an unknown frequency
            var unknownFreq = (BillingFrequency)999;
            var previous = D(2023, 1, 1);
            var candidate = D(2023, 1, 2);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RecurrenceScheduleValidator.IsExpectedNextDate(previous, candidate, unknownFreq));
        }

        [Fact]
        public void IsExpectedNextDate_ThrowsWhenPreviousDateOutOfRange()
        {
            // DateOnly values are bounded; adding a day to MaxValue is out of range.
            var max = DateOnly.MaxValue;
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                RecurrenceScheduleValidator.IsExpectedNextDate(max, max, BillingFrequency.Daily));
        }
    }
}
