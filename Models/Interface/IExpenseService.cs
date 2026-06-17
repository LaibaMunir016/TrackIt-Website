using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectWeb.Models;

namespace ProjectWeb.Interface
{
    public interface IExpenseService
    {
        void AddExpense(Expense expense);
        void UpdateExpense(Expense expense);
        void DeleteExpense(int id);
        public Task<Dictionary<int, decimal>> GetCategoryTotalsAsync(string userId);
        Task<IEnumerable<Expense>> GetExpenseHistoryAsync(string userId);
        Task<decimal> GetTotalMonthlyExpensesAsync(string userId);
        Task<IEnumerable<Expense>> GetExpensesByCategoryAsync(string userId, int categoryId);

    }
}