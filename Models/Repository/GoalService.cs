using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore; // Required for ToListAsync and FirstOrDefaultAsync
using ProjectWeb.Models;
using ProjectWeb.Interface;
using System.Threading.Tasks;
using ProjectWeb.Data;
using System.Linq;

namespace ProjectWeb.Models.Repository
{
    public class GoalService : IGoalService
    {
        private readonly ApplicationDbContext _context;

        public GoalService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Goal>> GetAllGoalsAsync(string userId)
        {
            // Filters goals by the specific User ID
            return await _context.Goals
                .Where(g => g.UserId == userId)
                .ToListAsync();
        }

        public async Task<Goal> GetGoalByIdAsync(int goalId)
        {
            // Finds a specific goal or returns null if not found
            return await _context.Goals
                .FirstOrDefaultAsync(g => g.GoalId == goalId);
        }

        public async Task AddGoalAsync(Goal goal)
        {
            await _context.Goals.AddAsync(goal);
        }

        public async Task UpdateGoalAsync(Goal goal)
        {
            // Logic: Auto-check if goal is achieved
            if (goal.CurrentAmount >= goal.TargetAmount)
            {
                goal.IsAchieved = true;
                goal.CurrentAmount = goal.TargetAmount; // Cap at target amount
            }
            else
            {
                goal.IsAchieved = false;
            }
            
            _context.Goals.Update(goal);
            await _context.SaveChangesAsync(); // Save changes here
        }

        public async Task DeleteGoalAsync(int goalId)
        {
            var goal = await _context.Goals.FindAsync(goalId);
            if (goal != null)
            {
                _context.Goals.Remove(goal);
            }
        }

        public async Task SaveChangesAsync()
        {
            // This persists all changes (Add, Update, Delete) to the database
            await _context.SaveChangesAsync();
        }
    }
}