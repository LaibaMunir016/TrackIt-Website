using Microsoft.AspNetCore.Identity;
using ProjectWeb.Models;
using Microsoft.AspNetCore.Mvc;
using ProjectWeb.Data;
using ProjectWeb.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ProjectWeb.Hubs;
using System.Text;
using System.Security.Claims;

namespace ProjectWeb.Controllers
{
    public class FinanceTrackerController : Controller
    {
        private readonly IHubContext<NotificationHub> _hub;
        private readonly ApplicationDbContext _context;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IIncomeService _is;
        private readonly IExpenseService _ie;
        private readonly IGoalService _goalService;

        public FinanceTrackerController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IIncomeService incomeService,
            IExpenseService expenseService,
            IGoalService goalService,
            IHubContext<NotificationHub> hub)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _is = incomeService;
            _ie = expenseService;
            _goalService = goalService;
            _hub = hub;
        }
        [AllowAnonymous]
        public IActionResult Home() => View();
        
        public IActionResult TermsOfServices() => View();
        public IActionResult PrivacyPolicy() => View();

        [AllowAnonymous]
        public IActionResult SignUp() => View();
        [AllowAnonymous]
        public IActionResult Help()=> View();

        [AllowAnonymous]
        public IActionResult About()=> View();
        
        [AllowAnonymous]
        public IActionResult Blog() => View();


        //    Profile
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Home");

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(string FullName, string PhoneNumber, string Currency)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Home");

            user.Currency = Currency;

            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                TempData["Error"] = string.Join(" | ", result.Errors.Select(e => e.Description));
                return RedirectToAction("Profile");
            }

            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }



        //Dashboard

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Home");

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Home");

            ViewBag.FullName = user?.FullName;
            ViewBag.Currency = user?.Currency ?? "Rs.";

            // if no income yet, go add income

            bool hasIncome = await _context.Incomes.AnyAsync(i => i.UserID == userId);
            if (!hasIncome) return RedirectToAction("AddIncome");

            var startOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

            // ---- Monthly breakdown (for doughnut chart) ----
            var breakdown = await _context.Expenses
                .Where(e => e.UserId == userId && e.DateSpent >= startOfMonth)
                .Include(e => e.Category)
                .GroupBy(e => e.Category != null ? e.Category.Name : "Uncategorized")
                .Select(g => new
                {
                    CategoryName = g.Key,
                    TotalAmount = g.Sum(x => x.Amount)
                })
                .ToListAsync();

            ViewBag.BreakdownLabels = breakdown.Select(x => x.CategoryName).ToList(); //Category Names
            ViewBag.BreakdownAmounts = breakdown.Select(x => x.TotalAmount).ToList(); //Total Amount of each category

            // Recent expenses for line chart 
            var chartTake = 15;

            var chartExpenses = await _context.Expenses
                .Where(e => e.UserId == userId)
                .Include(e => e.Category)
                .OrderByDescending(e => e.DateSpent)
                .Take(chartTake)
                .ToListAsync();

            chartExpenses = chartExpenses.OrderBy(e => e.DateSpent).ToList();

            ViewBag.RecentLabels = chartExpenses.Select(e => e.DateSpent.ToString("dd MMM")).ToList();
            ViewBag.RecentAmounts = chartExpenses.Select(e => e.Amount).ToList();
            ViewBag.RecentCategories = chartExpenses.Select(e => e.Category?.Name ?? "General").ToList();

            // ---- Totals/cards ----
            decimal totalIncome = await _is.GetCurrentMonthIncomeAsync(userId);
            decimal totalExpenses = await _ie.GetTotalMonthlyExpensesAsync(userId);
            ViewBag.income = totalIncome;
            ViewBag.Expenses = totalExpenses;
            decimal balance = totalIncome - totalExpenses;
            ViewBag.CurrentBalance = balance;
            if (balance < 0)
            {
                ViewBag.CurrentBalance = 0;
                ViewBag.Debt = Math.Abs(balance); // Store as a positive number for display
                ViewBag.IsDebt = true;
            }
            else
            {
                ViewBag.CurrentBalance = balance;
                ViewBag.Debt = 0;
                ViewBag.IsDebt = false;
            }


            return View();
        }
        [Authorize(Policy = "PremiumOnly")]
        public IActionResult Reminders()
        {
            return View();
        }

        [Authorize(Policy = "PremiumOnly")]
        [HttpGet]
        public IActionResult DownloadReminderIcs(string title, string startUtcIso, int minutesBefore = 10)
        {
            if (string.IsNullOrWhiteSpace(title)) return BadRequest("Missing title");
            if (string.IsNullOrWhiteSpace(startUtcIso)) return BadRequest("Missing startUtcIso");

            if (!DateTimeOffset.TryParse(startUtcIso, out var startUtc))
                return BadRequest("Invalid startUtcIso");

            // sanity
            if (minutesBefore < 0) minutesBefore = 0;
            if (minutesBefore > 1440) minutesBefore = 1440; // max 24h

            var endUtc = startUtc.AddMinutes(15);

            string fmt(DateTimeOffset dt) => dt.UtcDateTime.ToString("yyyyMMdd'T'HHmmss'Z'");
            string esc(string s) => s.Replace("\r", " ").Replace("\n", " ").Trim();

            var uid = Guid.NewGuid().ToString();

            var ics = $@"BEGIN:VCALENDAR
            VERSION:2.0
            PRODID:-//TrackIt//EN
            CALSCALE:GREGORIAN
            METHOD:PUBLISH
            BEGIN:VEVENT
            UID:{uid}
            DTSTAMP:{fmt(DateTimeOffset.UtcNow)}
            DTSTART:{fmt(startUtc)}
            DTEND:{fmt(endUtc)}
            SUMMARY:{esc(title)}
            BEGIN:VALARM
            TRIGGER:-PT{minutesBefore}M
            ACTION:DISPLAY
            DESCRIPTION:{esc(title)}
            END:VALARM
            END:VEVENT
            END:VCALENDAR";

            var bytes = Encoding.UTF8.GetBytes(ics);
            return File(bytes, "text/calendar", "TrackIt-Reminder.ics");
        }
        
        

       //Income

        [HttpGet]
        [Authorize]
        public IActionResult AddIncome() => View();

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddIncome(string currency, decimal amount, DateTime date, string source, string description)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Home");

            user.Currency = currency;
            await _userManager.UpdateAsync(user);
            var userId = _userManager.GetUserId(User);

            Income income = new Income
            {
                Amount = amount,
                DateReceived = date,
                Source = source,
                Description = description,
                UserID = userId
            };

            _is.InsertIncome(income);
            return RedirectToAction("Dashboard");
        }

        //Expenses

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Expenses()
        {
            var userId = _userManager.GetUserId(User);
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Home");
            
            var categories = await _context.Categories
                .Where(c => c.UserID == userId)
                .OrderBy(c => c.Name)
                .Select(c => new { c.Id, c.Name, c.Icon })
                .ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.Currency = user.Currency;
            ViewBag.TotalMonthly = await _ie.GetTotalMonthlyExpensesAsync(userId);
            ViewBag.CategoryTotals = await _ie.GetCategoryTotalsAsync(userId);
            var allExpenses = await _ie.GetExpenseHistoryAsync(userId);
            ViewBag.AllExpenses = allExpenses;
            return View();
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateCategory(string CategoryName)
        {
            if (string.IsNullOrWhiteSpace(CategoryName))
                return RedirectToAction("Expenses");

            var userId = _userManager.GetUserId(User);
            var name = CategoryName.Trim();

            var exists = await _context.Categories.AnyAsync(c =>
                c.UserID == userId && c.Name.ToLower() == name.ToLower());

            if (!exists)
            {
                _context.Categories.Add(new Category
                {
                    Name = name,
                    Icon = "bi-tag",
                    UserID = userId,
                    IsDefault = false
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Expenses");
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> AddExpense(Expense model)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return RedirectToAction("Home");

            // Ensure userID is set for that expense
            model.UserId = userId;

            //validation
            if (model.Amount <= 0) return RedirectToAction("Expenses");

            // computation
            var income = await _is.GetCurrentMonthIncomeAsync(userId);
            var expensesBefore = await _ie.GetTotalMonthlyExpensesAsync(userId);
            var balanceBefore = income - expensesBefore;

            // --- save expense ---
            _ie.AddExpense(model);

            // --- compute AFTER ---
            
            var expensesAfter = await _ie.GetTotalMonthlyExpensesAsync(userId);
            var balanceAfter = income - expensesAfter;

            //  crossing into debt
            if (balanceBefore >= 0 && balanceAfter < 0)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null)
                {
                    int currentMonth = (DateTime.UtcNow.Year * 100) + DateTime.UtcNow.Month;

                    // notify only once per month
                    bool alreadyNotifiedThisMonth = (user.DebtNotifiedMonth == currentMonth);

                    if (!alreadyNotifiedThisMonth)
                    {
                        user.IsInDebt = true;
                        user.DebtNotifiedMonth = currentMonth;
                        await _userManager.UpdateAsync(user);

                        await _hub.Clients.User(userId).SendAsync("DebtAlert", new
                        {
                            message = $"Your expenses crossed your income. Current balance: Rs {balanceAfter:N0}"
                        });
                    }
                }
            }
            else if (balanceAfter >= 0)
            {
                var user = await _userManager.GetUserAsync(User);
                if (user != null && user.IsInDebt)
                {
                    user.IsInDebt = false;
                    await _userManager.UpdateAsync(user);
                }
            }

            return RedirectToAction("Expenses");
        }

        [Authorize]
        public IActionResult DeleteExpense(int id)
        {
            _ie.DeleteExpense(id);
            return RedirectToAction("Expenses");
        }

        // GOALS SECTION

        [Authorize]
        public async Task<IActionResult> Goals()
        {
            var userId = _userManager.GetUserId(User);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            // Get user info for currency
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Home");
            
            ViewBag.Currency = user.Currency;
            string userCurrency = user?.Currency ?? "PKR";

            // Get user's goals using GoalService
            var goals = await _goalService.GetAllGoalsAsync(userId);

            // Order goals by StartDate (descending)
            var orderedGoals = goals.OrderByDescending(g => g.StartDate).ToList();

            // Calculate current balance using your existing services
            decimal totalIncome = await _is.GetCurrentMonthIncomeAsync(userId);
            decimal totalExpenses = await _ie.GetTotalMonthlyExpensesAsync(userId);

            decimal currentBalance = totalIncome - totalExpenses;

            // Pass data to view
            ViewBag.CurrentBalance = currentBalance;
            ViewBag.Currency = userCurrency;
            ViewBag.TotalGoals = orderedGoals.Count;
            ViewBag.CompletedGoals = orderedGoals.Count(g => g.IsAchieved);

            return View(orderedGoals);
        }

        [HttpGet]
        [Authorize]
        public IActionResult CreateGoal()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> CreateGoal(string GoalName, decimal TargetAmount, string? Description, DateTime Deadline)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(GoalName) || TargetAmount <= 0)
                return RedirectToAction("Goals");

            var start = DateTime.Today;

            var goal = new Goal
            {
                UserId = userId,
                Title = GoalName.Trim(),
                TargetAmount = TargetAmount,
                CurrentAmount = 0,
                StartDate = start,
                TargetDate = Deadline,
                Notes = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim(),
                IsAchieved = false,
                AchievedDate = null
            };

            _context.Goals.Add(goal);
            await _context.SaveChangesAsync();

            return RedirectToAction("Goals");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> EditGoal(int id)
        {
            var userId = _userManager.GetUserId(User);
            var goal = await _goalService.GetGoalByIdAsync(id);

            if (goal == null || goal.UserId != userId)
            {
                return NotFound();
            }

            return View(goal);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> EditGoal(int GoalId, string Title, decimal TargetAmount, DateTime TargetDate, string? Notes)
        {
            var userId = _userManager.GetUserId(User);
            var goal = await _goalService.GetGoalByIdAsync(GoalId);

            if (goal == null || goal.UserId != userId)
            {
                return NotFound();
            }

            if (!string.IsNullOrWhiteSpace(Title) && TargetAmount > 0 && TargetDate > DateTime.Today)
            {
                goal.Title = Title;
                goal.TargetAmount = TargetAmount;
                goal.TargetDate = TargetDate;
                goal.Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim();

                // Update achievement status (GoalService will handle this in UpdateGoalAsync)
                await _goalService.UpdateGoalAsync(goal);
                await _goalService.SaveChangesAsync();

                TempData["Success"] = $"Goal '{Title}' updated successfully!";
            }

            return RedirectToAction("Goals");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> DeleteGoal(int id)
        {
            var userId = _userManager.GetUserId(User);
            var goal = await _goalService.GetGoalByIdAsync(id);

            if (goal != null && goal.UserId == userId)
            {
                await _goalService.DeleteGoalAsync(id);
                await _goalService.SaveChangesAsync();

                TempData["Success"] = $"Goal '{goal.Title}' deleted successfully!";
            }

            return RedirectToAction("Goals");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> AddToGoal(int id)
        {
            var userId = _userManager.GetUserId(User);
            var goal = await _goalService.GetGoalByIdAsync(id);

            if (goal == null || goal.UserId != userId || goal.IsAchieved)
            {
                return NotFound();
            }

            ViewBag.Goal = goal;
            ViewBag.MaxAmount = goal.TargetAmount - goal.CurrentAmount;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddToGoal(int GoalId, decimal Amount)
        {
            var userId = _userManager.GetUserId(User);
            var goal = await _goalService.GetGoalByIdAsync(GoalId);

            if (goal == null || goal.UserId != userId || Amount <= 0)
            {
                TempData["Error"] = "Invalid amount or goal.";
                return RedirectToAction("Goals");
            }

            // Check if amount exceeds remaining target
            decimal remaining = goal.TargetAmount - goal.CurrentAmount;
            if (Amount > remaining)
            {
                Amount = remaining; // cap at remaining amount
                TempData["Info"] = $"Amount capped at {Amount:C} (remaining target).";
            }
            var wasAchieved = goal.IsAchieved;
            // Update the Goal progress
            goal.CurrentAmount += Amount;
            // if it just reached/exceeded target now, set AchievedDate ONCE
            if (!wasAchieved && goal.CurrentAmount >= goal.TargetAmount)
            {
                goal.AchievedDate = DateTime.Now;
            }

            // GoalService.UpdateGoalAsync will handle the IsAchieved check automatically
            await _goalService.UpdateGoalAsync(goal);

            // 2. Create a savings expense record
            var savingsCat = await _context.Categories
                .FirstOrDefaultAsync(c => c.UserID == userId && c.Name == "Savings & Goals");

            if (savingsCat == null)
            {
                savingsCat = new Category
                {
                    Name = "Savings & Goals",
                    UserID = userId,
                    Icon = "bi-piggy-bank",
                    IsDefault = false
                };
                _context.Categories.Add(savingsCat);
                await _context.SaveChangesAsync();
            }

            // Create expense record for the savings
            var deduction = new Expense
            {
                UserId = userId,
                Amount = Amount,
                DateSpent = DateTime.Now,
                CategoryId = savingsCat.Id,
                Description = $"Saved for goal: {goal.Title}",
                PaymentMethod = "Internal Transfer"
            };

            _context.Expenses.Add(deduction);

            // Save both changes
            await _goalService.SaveChangesAsync();
            await _context.SaveChangesAsync();

            if (goal.IsAchieved)
            {
                TempData["Success"] = $"🎉 Congratulations! You've achieved your goal: '{goal.Title}'!";
            }
            else
            {
                TempData["Success"] = $"Successfully added {Amount:C} to '{goal.Title}'!";
            }

            return RedirectToAction("Goals");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ResetGoalProgress(int id)
        {
            var userId = _userManager.GetUserId(User);
            var goal = await _goalService.GetGoalByIdAsync(id);

            if (goal != null && goal.UserId == userId && goal.CurrentAmount > 0)
            {
                // Remove the saved amount from savings category
                var savingsCat = await _context.Categories
                    .FirstOrDefaultAsync(c => c.UserID == userId && c.Name == "Savings & Goals");

                if (savingsCat != null)
                {
                    // Create a negative expense to add back to balance
                    var refund = new Expense
                    {
                        UserId = userId,
                        Amount = -goal.CurrentAmount, // Negative amount adds back to balance
                        DateSpent = DateTime.Now,
                        CategoryId = savingsCat.Id,
                        Description = $"Reset progress for goal: {goal.Title}",
                        PaymentMethod = "Internal Transfer"
                    };

                    _context.Expenses.Add(refund);
                    await _context.SaveChangesAsync();
                }

                // Reset goal progress
                goal.CurrentAmount = 0;
                goal.IsAchieved = false;

                await _goalService.UpdateGoalAsync(goal);
                await _goalService.SaveChangesAsync();

                TempData["Success"] = $"Goal '{goal.Title}' progress reset!";
            }

            return RedirectToAction("Goals");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> MarkGoalAsAchieved(int id)
        {
            var userId = _userManager.GetUserId(User);
            var goal = await _goalService.GetGoalByIdAsync(id);

            if (goal != null && goal.UserId == userId && !goal.IsAchieved)
            {
                // Set to target amount and mark as achieved
                goal.CurrentAmount = goal.TargetAmount;
                goal.IsAchieved = true;
                goal.AchievedDate = DateTime.Now;

                await _goalService.UpdateGoalAsync(goal);
                await _goalService.SaveChangesAsync();

                TempData["Success"] = $"Goal '{goal.Title}' marked as achieved!";
            }

            return RedirectToAction("Goals");
        }
        
        [HttpGet]
        [Authorize]
        public IActionResult Upgrade(string? returnUrl = null)
        {
            ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl;
            return View();
        }

        // simple upgrade: grants premium claim and redirects back
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ActivatePremium(string? returnUrl = null)
        {
            var user = await _userManager.GetUserAsync(User);
            
            if (user == null) 
            {
                return Json(new { success = false, message = "User not found. Please log in again." });
            }

            try
            {
                // Remove existing plan claim(s) to avoid duplicates
                var claims = await _userManager.GetClaimsAsync(user);
                var planClaims = claims.Where(c => c.Type == "plan").ToList();

                //  Add premium claim
                await _userManager.AddClaimAsync(user, new Claim("plan", "premium"));

                //  Refresh cookie so premium works immediately
                await _signInManager.RefreshSignInAsync(user);

                //  Determine the redirect destination
                string destination = "/";
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    destination = returnUrl;
                }
                else
                {
                    destination = Url.Action("Dashboard", "FinanceTracker") ?? "/";
                }

                // Return JSON success with the destination URL
                return Json(new { success = true, redirectUrl = destination });
            }
            catch (Exception ex)
            {
                // Log the error (ex) here if you have a logger
                return Json(new { success = false, message = "An error occurred during activation." });
            }
        }

        [Authorize]
        public async Task<IActionResult> Reports(string period = "monthly", string reportType = "overview")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var userId = user.Id;
            var now = DateTime.Now;

            // Calculate date ranges based on period
            DateTime startDate, endDate = now;

            switch (period.ToLower())
            {
                case "weekly":
                    startDate = now.AddDays(-7);
                    break;
                case "monthly":
                    startDate = new DateTime(now.Year, now.Month, 1);
                    break;
                case "quarterly":
                    var quarter = (now.Month - 1) / 3 + 1;
                    startDate = new DateTime(now.Year, (quarter - 1) * 3 + 1, 1);
                    break;
                case "yearly":
                    startDate = new DateTime(now.Year, 1, 1);
                    break;
                default:
                    startDate = now.AddMonths(-12);
                    break;
            }

            // Fetch data with date range
            var userIncomes = _context.Incomes
                .Where(x => x.UserID == userId && x.DateReceived >= startDate && x.DateReceived <= endDate)
                .OrderBy(x => x.DateReceived)
                .ToList();

            var userExpenses = _context.Expenses
                .Where(x => x.UserId == userId && x.DateSpent >= startDate && x.DateSpent <= endDate)
                .Include(x => x.Category)
                .OrderBy(x => x.DateSpent)
                .ToList();

            // Calculate totals
            decimal totalIncome = userIncomes.Sum(x => x.Amount);
            decimal totalExpense = userExpenses.Sum(x => x.Amount);
            decimal balance = totalIncome - totalExpense;

            // Get category breakdown
            var categoryBreakdown = userExpenses
                .GroupBy(x => x.Category?.Name ?? "Uncategorized")
                .Select(g => new
                {
                    Category = g.Key,
                    Amount = g.Sum(x => x.Amount),
                    Percentage = totalExpense > 0 ? (g.Sum(x => x.Amount) / totalExpense) * 100 : 0,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Amount)
                .ToList();

            // Get monthly trend data
            var monthlyData = userIncomes
                .GroupBy(x => new { x.DateReceived.Year, x.DateReceived.Month })
                .Select(g => new
                {
                    YearMonth = $"{g.Key.Year}-{g.Key.Month:00}",
                    Income = g.Sum(x => x.Amount),
                    Expenses = userExpenses
                        .Where(e => e.DateSpent.Year == g.Key.Year && e.DateSpent.Month == g.Key.Month)
                        .Sum(e => e.Amount)
                })
                .OrderBy(x => x.YearMonth)
                .ToList();

            // Get weekly spending pattern
            var weeklyPattern = Enumerable.Range(0, 7)
                .Select(day => new
                {
                    Day = ((DayOfWeek)day).ToString(),
                    Amount = userExpenses
                        .Where(e => e.DateSpent.DayOfWeek == (DayOfWeek)day)
                        .Sum(e => e.Amount)
                })
                .ToList();

            // Calculate financial metrics
            var metrics = new
            {
                SavingsRate = totalIncome > 0 ? (balance / totalIncome) * 100 : 0,
                ExpenseRatio = totalIncome > 0 ? (totalExpense / totalIncome) * 100 : 0,
                AverageMonthlyIncome = totalIncome / 12,
                AverageMonthlyExpense = totalExpense / 12,
                TopCategory = categoryBreakdown.FirstOrDefault()?.Category ?? "None",
                TopCategoryAmount = categoryBreakdown.FirstOrDefault()?.Amount ?? 0,
                MonthlyGrowth = CalculateGrowthRate(monthlyData.Select(x => x.Income).ToList())
            };

            // Pass data to view
            ViewBag.TotalIncome = totalIncome;
            ViewBag.TotalExpense = totalExpense;
            ViewBag.Currency = user.Currency ?? "PKR";
            ViewBag.Balance = balance;
            ViewBag.Period = period;
            ViewBag.ReportType = reportType;

            // Chart data
            ViewBag.ChartLabels = monthlyData.Select(x => x.YearMonth).ToArray();
            ViewBag.IncomeData = monthlyData.Select(x => x.Income).ToArray();
            ViewBag.ExpenseData = monthlyData.Select(x => x.Expenses).ToArray();

            // Category data
            ViewBag.CategoryLabels = categoryBreakdown.Select(x => x.Category).ToArray();
            ViewBag.CategoryData = categoryBreakdown.Select(x => x.Amount).ToArray();
            ViewBag.CategoryPercentages = categoryBreakdown.Select(x => x.Percentage).ToArray();

            // Weekly data
            ViewBag.WeeklyLabels = weeklyPattern.Select(x => x.Day).ToArray();
            ViewBag.WeeklyData = weeklyPattern.Select(x => x.Amount).ToArray();

            // Metrics
            ViewBag.Metrics = metrics;

            // Insights
            ViewBag.Insights = GenerateInsights(totalIncome, totalExpense, balance, categoryBreakdown);

            return View();
        }

        private decimal CalculateGrowthRate(List<decimal> values)
        {
            if (values.Count < 2) return 0;

            var first = values.First();
            var last = values.Last();

            if (first == 0) return 0;

            return ((last - first) / first) * 100;
        }

        private List<string> GenerateInsights(decimal income, decimal expense, decimal balance, dynamic categoryBreakdown)
        {
            var insights = new List<string>();

            decimal savingsRate = income > 0 ? (balance / income) * 100 : 0;
            decimal expenseRatio = income > 0 ? (expense / income) * 100 : 0;

            if (savingsRate < 10)
                insights.Add($"Your savings rate is low ({savingsRate:F1}%). Aim for at least 20% for better financial health.");

            if (expenseRatio > 80)
                insights.Add($"Your expense ratio is high ({expenseRatio:F1}%). Consider reducing discretionary spending.");

            var topCategory = ((IEnumerable<dynamic>)categoryBreakdown).FirstOrDefault();
            if (topCategory != null && topCategory.Percentage > 30)
                insights.Add($"Your spending on {topCategory.Category} is high ({topCategory.Percentage:F1}% of expenses). Consider budgeting for this category.");

            if (balance > 0 && savingsRate > 15)
                insights.Add($"Great job! Your savings rate of {savingsRate:F1}% is above the recommended level.");

            return insights;
        }
        
        [Authorize]
        public async Task<IActionResult> GetCategoryDetails(int categoryId)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var expenses = await _ie.GetExpensesByCategoryAsync(userId, categoryId);

            return PartialView("_ExpenseList", expenses.ToList());
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetExpenseHistoryPartial()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var expenses = (await _ie.GetExpenseHistoryAsync(userId)).ToList(); // start se ab tak
            return PartialView("_ExpenseList", expenses);
        }
    }
}