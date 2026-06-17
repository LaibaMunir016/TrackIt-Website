using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProjectWeb.Data;
using ProjectWeb.Models;

namespace ProjectWeb.ViewComponents
{
    public class RecentTransactionsViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RecentTransactionsViewComponent(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync(int take = 10)
        {
            var userId = _userManager.GetUserId(HttpContext.User);

            if (string.IsNullOrEmpty(userId))
                return View(new List<Expense>());

            var recent = await _context.Expenses
                .Where(e => e.UserId == userId)
                .Include(e => e.Category) //  so expense.Category.Name works
                .OrderByDescending(e => e.DateSpent)
                .Take(take)
                .ToListAsync();

            return View(recent);
        }
    }
}
