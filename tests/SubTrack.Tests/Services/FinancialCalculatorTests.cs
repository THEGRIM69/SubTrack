using System;
using System.Collections.Generic;
using Xunit;
using SubTrack.Core.Services;

namespace SubTrack.Tests.Services
{
    public class FinancialCalculatorTests
    {
        [Fact]
        public void CalculateRealizedExpenses_SinglePayment_ReturnsAmount()
        {
            var payments = new List<long> { 1000 };
            var expenses = new List<long>();
            var result = FinancialCalculator.CalculateRealizedExpenses(payments, expenses);
            Assert.Equal(1000, result);
        }

        [Fact]
        public void CalculateRealizedExpenses_MultiplePayments_ReturnsSum()
        {
            var payments = new List<long> { 1000, 2000, 3000 };
            var expenses = new List<long>();
            var result = FinancialCalculator.CalculateRealizedExpenses(payments, expenses);
            Assert.Equal(6000, result);
        }

        [Fact]
        public void CalculateRealizedExpenses_MultipleExpenses_ReturnsSum()
        {
            var payments = new List<long>();
            var expenses = new List<long> { 500, 1500 };
            var result = FinancialCalculator.CalculateRealizedExpenses(payments, expenses);
            Assert.Equal(2000, result);
        }

        [Fact]
        public void CalculateRealizedExpenses_PaymentsAndExpenses_ReturnsCombinedSum()
        {
            var payments = new List<long> { 1000, 2000 };
            var expenses = new List<long> { 500, 1500 };
            var result = FinancialCalculator.CalculateRealizedExpenses(payments, expenses);
            Assert.Equal(5000, result);
        }

        [Fact]
        public void CalculateRealizedExpenses_EmptyCollections_ReturnsZero()
        {
            var payments = new List<long>();
            var expenses = new List<long>();
            var result = FinancialCalculator.CalculateRealizedExpenses(payments, expenses);
            Assert.Equal(0, result);
        }

        [Fact]
        public void CalculateMonthlyBalance_PositiveResult()
        {
            long income = 10000;
            long expenses = 4000;
            var balance = FinancialCalculator.CalculateMonthlyBalance(income, expenses);
            Assert.Equal(6000, balance);
        }

        [Fact]
        public void CalculateMonthlyBalance_ZeroResult()
        {
            long income = 5000;
            long expenses = 5000;
            var balance = FinancialCalculator.CalculateMonthlyBalance(income, expenses);
            Assert.Equal(0, balance);
        }

        [Fact]
        public void CalculateMonthlyBalance_NegativeResult()
        {
            long income = 3000;
            long expenses = 5000;
            var balance = FinancialCalculator.CalculateMonthlyBalance(income, expenses);
            Assert.Equal(-2000, balance);
        }

        [Fact]
        public void CalculateRealizedExpenses_LargeValues_NoOverflow()
        {
            var payments = new List<long> { long.MaxValue - 1 };
            var expenses = new List<long> { 1 };
            var result = FinancialCalculator.CalculateRealizedExpenses(payments, expenses);
            Assert.Equal(long.MaxValue, result);
        }

        [Fact]
        public void CalculateRealizedExpenses_Overflow_Throws()
        {
            var payments = new List<long> { long.MaxValue };
            var expenses = new List<long> { 1 };
            Assert.Throws<OverflowException>(() => FinancialCalculator.CalculateRealizedExpenses(payments, expenses));
        }

        [Fact]
        public void CalculateMonthlyBalance_Overflow_Throws()
        {
            long income = long.MinValue;
            long expenses = 1;
            Assert.Throws<OverflowException>(() => FinancialCalculator.CalculateMonthlyBalance(income, expenses));
        }

        [Fact]
        public void CalculateRealizedExpenses_NullPayments_Throws()
        {
            List<long>? payments = null;
            var expenses = new List<long> { 100 };
            Assert.Throws<ArgumentNullException>(() => FinancialCalculator.CalculateRealizedExpenses(payments!, expenses));
        }

        [Fact]
        public void CalculateRealizedExpenses_NullExpenses_Throws()
        {
            var payments = new List<long> { 100 };
            List<long>? expenses = null;
            Assert.Throws<ArgumentNullException>(() => FinancialCalculator.CalculateRealizedExpenses(payments, expenses!));
        }
    }
}
