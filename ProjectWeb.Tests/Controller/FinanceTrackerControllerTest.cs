using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using ProjectWeb.Controllers;
using ProjectWeb.Data;
using ProjectWeb.Hubs;
using ProjectWeb.Interface;
using ProjectWeb.Models;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace ProjectWeb.Tests.Controllers
{
    public class FinanceTrackerControllerTests
    {
        private readonly Mock<IIncomeService> _mockIncomeService;
        private readonly Mock<IExpenseService> _mockExpenseService;
        private readonly Mock<IGoalService> _mockGoalService;
        private readonly Mock<IHubContext<NotificationHub>> _mockHub;
        private readonly Mock<UserManager<ApplicationUser>> _mockUserManager;
        private readonly FinanceTrackerController _controller;

        public FinanceTrackerControllerTests()
        {
            // 1. Setup Mocks for all dependencies
            _mockIncomeService = new Mock<IIncomeService>();
            _mockExpenseService = new Mock<IExpenseService>();
            _mockGoalService = new Mock<IGoalService>();
            _mockHub = new Mock<IHubContext<NotificationHub>>();
            
            var store = new Mock<IUserStore<ApplicationUser>>();
            _mockUserManager = new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);

            // 2. Initialize Controller (Passing null for DbContext/SignInManager as these tests don't touch them)
            _controller = new FinanceTrackerController(
                null, 
                _mockUserManager.Object,
                null, 
                _mockIncomeService.Object,
                _mockExpenseService.Object,
                _mockGoalService.Object,
                _mockHub.Object
            );

            // 3. Fake the User Identity (for methods using User.GetUserId)
            var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[] {
                new Claim(ClaimTypes.NameIdentifier, "test-user-123")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext()
            {
                HttpContext = new DefaultHttpContext() { User = user },
            };

            // 4. Fake TempData (to prevent null errors when setting Success/Error messages)
            _controller.TempData = new Mock<ITempDataDictionary>().Object;
        }

        // --- SECTION 1: SIMPLE NAVIGATION (GET METHODS) ---

        [Fact] public void Home_ReturnsView() => Assert.IsType<ViewResult>(_controller.Home());
        [Fact] public void Help_ReturnsView() => Assert.IsType<ViewResult>(_controller.Help());
        [Fact] public void About_ReturnsView() => Assert.IsType<ViewResult>(_controller.About());
        [Fact] public void Blog_ReturnsView() => Assert.IsType<ViewResult>(_controller.Blog());
        [Fact] public void SignUp_ReturnsView() => Assert.IsType<ViewResult>(_controller.SignUp());
        [Fact] public void CreateGoal_ReturnsView() => Assert.IsType<ViewResult>(_controller.CreateGoal());

        // --- SECTION 2: SERVICE-BASED LOGIC ---


        [Fact]
        public void DeleteExpense_CallsServiceAndRedirects()
        {
            var result = _controller.DeleteExpense(55);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Expenses", redirect.ActionName);
            _mockExpenseService.Verify(s => s.DeleteExpense(55), Times.Once);
        }

        [Fact]
        public async Task MarkGoalAsAchieved_UpdatesStatusAndRedirects()
        {
            var goal = new Goal { GoalId = 1, UserId = "test-user-123", IsAchieved = false };
            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("test-user-123");
            _mockGoalService.Setup(s => s.GetGoalByIdAsync(1)).ReturnsAsync(goal);

            var result = await _controller.MarkGoalAsAchieved(1);

            Assert.IsType<RedirectToActionResult>(result);
            _mockGoalService.Verify(s => s.UpdateGoalAsync(It.Is<Goal>(g => g.IsAchieved == true)), Times.Once);
        }

        // --- SECTION 3: ERROR HANDLING & EDGE CASES ---

        

        [Fact]
        public async Task AddToGoal_Post_RedirectsOnNegativeAmount()
        {
            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns("test-user-123");
            _mockGoalService.Setup(s => s.GetGoalByIdAsync(It.IsAny<int>())).ReturnsAsync(new Goal());

            var result = await _controller.AddToGoal(1, -100m);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Goals", redirect.ActionName);
            _mockGoalService.Verify(s => s.UpdateGoalAsync(It.IsAny<Goal>()), Times.Never);
        }

        [Fact]
        public async Task CreateCategory_RedirectsWhenNameEmpty()
        {
            var result = await _controller.CreateCategory("");
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Expenses", redirect.ActionName);
        }

        [Fact]
        public async Task Dashboard_RedirectsToHome_IfNoUserSession()
        {
            _mockUserManager.Setup(u => u.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns((string)null);

            var result = await _controller.Dashboard();

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Home", redirect.ActionName);
        }
    }
}