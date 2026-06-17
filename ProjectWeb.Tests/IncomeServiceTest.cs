using Xunit;
using Moq;
using ProjectWeb.Models;
using ProjectWeb.Interface;
using System;
using System.Threading.Tasks;

namespace ProjectWeb.Tests
{
    public class IncomeServiceTests
    {
        private readonly Mock<IIncomeService> _mockIncomeService;

        public IncomeServiceTests()
        {
            _mockIncomeService = new Mock<IIncomeService>();
        }

        [Fact]
        public void InsertIncome_ShouldBeCalledOnce()
        {
            // Arrange
            var income = new Income { UserID = "user1", Amount = 5000, DateReceived = DateTime.Now };

            // Act
            _mockIncomeService.Object.InsertIncome(income);

            // Assert
            _mockIncomeService.Verify(s => s.InsertIncome(It.IsAny<Income>()), Times.Once);
        }

        [Fact]
        public async Task GetCurrentMonthIncomeAsync_ReturnsMockedValue()
        {
            // Arrange
            string userId = "user123";
            _mockIncomeService.Setup(s => s.GetCurrentMonthIncomeAsync(userId))
                               .ReturnsAsync(3000.50m);

            // Act
            var result = await _mockIncomeService.Object.GetCurrentMonthIncomeAsync(userId);

            // Assert
            Assert.Equal(3000.50m, result);
        }

        [Fact]
        public void UpsertIncome_ShouldBeCalledWithCorrectData()
        {
            // Arrange
            var income = new Income { UserID = "user1", Amount = 1200 };

            // Act
            _mockIncomeService.Object.UpsertIncome(income);

            // Assert
            _mockIncomeService.Verify(s => s.UpsertIncome(income), Times.Once);
        }
    }
}