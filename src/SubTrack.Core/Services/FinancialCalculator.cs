using System;
using System.Collections.Generic;

namespace SubTrack.Core.Services
{
    /// <summary>
    /// Provides independent financial calculations without any external dependencies.
    /// All amounts are expressed in centavos (long) to avoid floating‑point inaccuracies.
    /// </summary>
    public static class FinancialCalculator
    {
        /// <summary>
        /// Calculates the total realized expenses for a period.
        /// It sums the amounts of valid payments and valid expenses.
        /// </summary>
        /// <param name="validPayments">Collection of payment amounts in centavos.</param>
        /// <param name="validExpenses">Collection of expense amounts in centavos.</param>
        /// <returns>The sum of all payments and expenses.</returns>
        /// <exception cref="ArgumentNullException">If either collection is null.</exception>
        /// <exception cref="OverflowException">If the sum exceeds the range of <see cref="long"/>.</exception>
        public static long CalculateRealizedExpenses(IEnumerable<long> validPayments, IEnumerable<long> validExpenses)
        {
            if (validPayments == null) throw new ArgumentNullException(nameof(validPayments));
            if (validExpenses == null) throw new ArgumentNullException(nameof(validExpenses));

            long total = 0;
            checked
            {
                foreach (var p in validPayments)
                {
                    total += p;
                }
                foreach (var e in validExpenses)
                {
                    total += e;
                }
            }
            return total;
        }

        /// <summary>
        /// Calculates the monthly balance (income minus expenses).
        /// </summary>
        /// <param name="totalIncomeCents">Total income for the month in centavos.</param>
        /// <param name="totalExpensesCents">Total realized expenses for the month in centavos.</param>
        /// <returns>The monthly balance, which may be positive, zero, or negative.</returns>
        /// <exception cref="OverflowException">If the subtraction overflows <see cref="long"/>.</exception>
        public static long CalculateMonthlyBalance(long totalIncomeCents, long totalExpensesCents)
        {
            checked
            {
                return totalIncomeCents - totalExpensesCents;
            }
        }
    }
}
