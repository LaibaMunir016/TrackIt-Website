using System.Collections.Generic;
using System.Threading.Tasks; // <--- Add this line
using ProjectWeb.Models;

namespace ProjectWeb.Interface
{
    public interface IGoalService
    {
        Task<IEnumerable<Goal>> GetAllGoalsAsync(string userId);
        Task<Goal> GetGoalByIdAsync(int goalId);
        Task AddGoalAsync(Goal goal);
        Task UpdateGoalAsync(Goal goal);
        Task DeleteGoalAsync(int goalId);
        Task SaveChangesAsync();
    }
}