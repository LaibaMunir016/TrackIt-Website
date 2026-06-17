using System.Collections.Generic;
using ProjectWeb.Models;

namespace ProjectWeb.Interface{
    public interface IIncomeService
    {
        public void InsertIncome(Income income);
        public Task<decimal> GetCurrentMonthIncomeAsync(string userId);
        public void UpsertIncome(Income income);
        
    }
}