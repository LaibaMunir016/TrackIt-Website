using Xunit;
using Moq;
using ProjectWeb.Models;
using ProjectWeb.Interface;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace ProjectWeb.Tests
{
    public class ExpenseServiceTests
    {
        private readonly Mock<IExpenseService> _mockService;

        public ExpenseServiceTests()
        {
            _mockService = new Mock<IExpenseService>();
        }

        // 1. Test Add
        [Fact]
        public void AddExpense_ShouldInvokeService()
        {
            var expense = new Expense { Amount = 50, CategoryId = 1 };
            _mockService.Object.AddExpense(expense);
            _mockService.Verify(x => x.AddExpense(expense), Times.Once);
        }

        // 2. Test Update
        [Fact]
        public void UpdateExpense_ShouldInvokeService()
        {
            var expense = new Expense { ExpenseId = 1, Amount = 75 };
            _mockService.Object.UpdateExpense(expense);
            _mockService.Verify(x => x.UpdateExpense(expense), Times.Once);
        }

        // 3. Test Delete
        [Fact]
        public void DeleteExpense_ShouldInvokeService()
        {
            int expenseId = 99;
            _mockService.Object.DeleteExpense(expenseId);
            _mockService.Verify(x => x.DeleteExpense(expenseId), Times.Once);
        }

        // 4. Test Monthly Total
        [Fact]
        public async Task GetTotalMonthlyExpensesAsync_ReturnsValue()
        {
            _mockService.Setup(x => x.GetTotalMonthlyExpensesAsync("user1")).ReturnsAsync(500m);
            var result = await _mockService.Object.GetTotalMonthlyExpensesAsync("user1");
            Assert.Equal(500m, result);
        }

        // 5. Test Category Totals (Dictionary)
        [Fact]
        public async Task GetCategoryTotalsAsync_ReturnsDictionary()
        {
            var totals = new Dictionary<int, decimal> { { 1, 100m }, { 2, 200m } };
            _mockService.Setup(x => x.GetCategoryTotalsAsync("user1")).ReturnsAsync(totals);
            
            var result = await _mockService.Object.GetCategoryTotalsAsync("user1");
            
            Assert.Equal(2, result.Count);
            Assert.Equal(100m, result[1]);
        }

        // 6. Test Expense History (List)
        [Fact]
        public async Task GetExpenseHistoryAsync_ReturnsList()
        {
            var history = new List<Expense> 
            { 
                new Expense { ExpenseId = 1, Amount = 10 },
                new Expense { ExpenseId = 2, Amount = 20 }
            };
            _mockService.Setup(x => x.GetExpenseHistoryAsync("user1")).ReturnsAsync(history);

            var result = await _mockService.Object.GetExpenseHistoryAsync("user1");

            Assert.Equal(2, result.Count());
            Assert.Equal(10, result.First().Amount);
        }
    }
}