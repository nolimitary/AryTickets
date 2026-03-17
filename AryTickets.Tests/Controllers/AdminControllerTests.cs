using AryTickets.Controllers;
using AryTickets.Models;
using AryTickets.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace AryTickets.Tests.Controllers
{
    public class AdminControllerTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly Mock<RoleManager<IdentityRole>> _roleManager;
        private readonly Mock<IHttpClientFactory> _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AdminControllerTests()
        {
            _userManager = MockHelpers.MockUserManager();
            _roleManager = MockHelpers.MockRoleManager();
            _httpClientFactory = new Mock<IHttpClientFactory>();
            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "TMDb:ApiKey", "test-api-key" }
                })
                .Build();
        }

        [Fact]
        public async Task Index_ReturnsViewWithDashboardData()
        {
            var context = TestDbContextFactory.CreateWithData();
            _userManager.Setup(m => m.Users)
                .Returns(context.Users);

            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var result = await controller.Index() as ViewResult;

            Assert.NotNull(result);
            Assert.NotNull(controller.ViewData["TotalBookings"]);
            Assert.Equal(2, (int)controller.ViewData["TotalBookings"]);
        }

        [Fact]
        public async Task Bookings_ReturnsAllBookings()
        {
            var context = TestDbContextFactory.CreateWithData();
            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var result = await controller.Bookings() as ViewResult;

            Assert.NotNull(result);
            var bookings = result.Model as List<Booking>;
            Assert.NotNull(bookings);
            Assert.Equal(2, bookings.Count);
        }

        [Fact]
        public async Task DeleteBooking_ExistingBooking_DeletesAndRedirects()
        {
            var context = TestDbContextFactory.CreateWithData();
            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var result = await controller.DeleteBooking(1);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Bookings", redirect.ActionName);
            Assert.Null(await context.Bookings.FindAsync(1));
        }

        [Fact]
        public async Task DeleteBooking_NonExistent_StillRedirects()
        {
            var context = TestDbContextFactory.Create();
            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var result = await controller.DeleteBooking(999);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Bookings", redirect.ActionName);
        }

        [Fact]
        public async Task ApproveApplication_SetsStatusAndMakesCritic()
        {
            var context = TestDbContextFactory.CreateWithData();
            var testUser = new ApplicationUser
            {
                Id = "test-user-id",
                UserName = "TestUser",
                Email = "test@test.com",
                IsCritic = false
            };
            _userManager.Setup(m => m.FindByIdAsync("test-user-id"))
                .ReturnsAsync(testUser);

            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var result = await controller.ApproveApplication(1);

            var app = await context.CriticApplications.FindAsync(1);
            Assert.Equal(ApplicationStatus.Approved, app.Status);
            Assert.NotNull(app.ReviewedAt);
            Assert.True(testUser.IsCritic);
        }

        [Fact]
        public async Task DenyApplication_SetsStatusToDenied()
        {
            var context = TestDbContextFactory.CreateWithData();
            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var result = await controller.DenyApplication(1);

            var app = await context.CriticApplications.FindAsync(1);
            Assert.Equal(ApplicationStatus.Denied, app.Status);
            Assert.NotNull(app.ReviewedAt);
        }

        [Fact]
        public async Task ApproveApplication_NonExistent_ReturnsNotFound()
        {
            var context = TestDbContextFactory.Create();
            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var result = await controller.ApproveApplication(999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task DenyApplication_NonExistent_ReturnsNotFound()
        {
            var context = TestDbContextFactory.Create();
            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var result = await controller.DenyApplication(999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Applications_FilterPending_ReturnsOnlyPending()
        {
            var context = TestDbContextFactory.CreateWithData();
            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var result = await controller.Applications("pending") as ViewResult;

            Assert.NotNull(result);
            var apps = result.Model as List<CriticApplication>;
            Assert.NotNull(apps);
            Assert.All(apps, a => Assert.Equal(ApplicationStatus.Pending, a.Status));
        }

        [Fact]
        public async Task Showtimes_ReturnsOnlyActiveShowtimes()
        {
            var context = TestDbContextFactory.CreateWithData();
            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var result = await controller.Showtimes() as ViewResult;

            Assert.NotNull(result);
            var showtimes = result.Model as List<Showtime>;
            Assert.NotNull(showtimes);
            Assert.All(showtimes, s => Assert.True(s.IsActive));
            Assert.DoesNotContain(showtimes, s => s.Id == 3); // Inactive showtime
        }

        [Fact]
        public async Task DeleteShowtime_SetsInactive()
        {
            var context = TestDbContextFactory.CreateWithData();
            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var result = await controller.DeleteShowtime(1);

            var showtime = await context.Showtimes.FindAsync(1);
            Assert.False(showtime.IsActive);
        }

        [Fact]
        public async Task CreateShowtime_ValidData_CreatesShowtime()
        {
            var context = TestDbContextFactory.Create();
            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var result = await controller.CreateShowtime(500, "New Movie", "/poster.jpg", "2026-04-01 19:00", "Hall 2", 15.00m);

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Showtimes", redirect.ActionName);
            Assert.Single(await context.Showtimes.ToListAsync());
        }

        [Fact]
        public async Task CreateShowtime_EmptyShowtimes_ReturnsBadRequest()
        {
            var context = TestDbContextFactory.Create();
            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var result = await controller.CreateShowtime(500, "Movie", "/poster.jpg", "", "Hall 1", 12.50m);
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task CreateShowtime_MultipleShowtimes_CreatesAll()
        {
            var context = TestDbContextFactory.Create();
            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var result = await controller.CreateShowtime(500, "Movie", "/poster.jpg",
                "2026-04-01 19:00,2026-04-02 19:00,2026-04-03 19:00", "Hall 1", 12.50m);

            Assert.Equal(3, await context.Showtimes.CountAsync());
        }

        [Fact]
        public async Task Analytics_ReturnsAnalyticsModel()
        {
            var context = TestDbContextFactory.CreateWithData();
            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var result = await controller.Analytics() as ViewResult;

            Assert.NotNull(result);
            var model = result.Model as AnalyticsViewModel;
            Assert.NotNull(model);
            Assert.Equal(37.50m, model.TotalRevenue);
            Assert.Equal(2, model.TotalBookings);
        }

        [Fact]
        public async Task ToggleAdmin_UserNotFound_ReturnsNotFound()
        {
            var context = TestDbContextFactory.Create();
            _userManager.Setup(m => m.FindByIdAsync("nonexistent"))
                .ReturnsAsync((ApplicationUser)null);

            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var result = await controller.ToggleAdmin("nonexistent");
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ToggleAdmin_UserIsAdmin_RemovesAdminRole()
        {
            var context = TestDbContextFactory.Create();
            var user = new ApplicationUser { Id = "user-id", UserName = "User" };
            _userManager.Setup(m => m.FindByIdAsync("user-id")).ReturnsAsync(user);
            _userManager.Setup(m => m.IsInRoleAsync(user, "Admin")).ReturnsAsync(true);
            _userManager.Setup(m => m.RemoveFromRoleAsync(user, "Admin"))
                .ReturnsAsync(IdentityResult.Success);

            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var result = await controller.ToggleAdmin("user-id");

            _userManager.Verify(m => m.RemoveFromRoleAsync(user, "Admin"), Times.Once);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Users", redirect.ActionName);
        }

        [Fact]
        public async Task ToggleAdmin_UserIsNotAdmin_AddsAdminRole()
        {
            var context = TestDbContextFactory.Create();
            var user = new ApplicationUser { Id = "user-id", UserName = "User" };
            _userManager.Setup(m => m.FindByIdAsync("user-id")).ReturnsAsync(user);
            _userManager.Setup(m => m.IsInRoleAsync(user, "Admin")).ReturnsAsync(false);
            _userManager.Setup(m => m.AddToRoleAsync(user, "Admin"))
                .ReturnsAsync(IdentityResult.Success);

            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var result = await controller.ToggleAdmin("user-id");

            _userManager.Verify(m => m.AddToRoleAsync(user, "Admin"), Times.Once);
        }

        [Fact]
        public async Task DeleteUser_CannotDeleteSelf()
        {
            var context = TestDbContextFactory.Create();
            var adminUser = new ApplicationUser { Id = "admin-user-id", UserName = "Admin" };
            _userManager.Setup(m => m.FindByIdAsync("admin-user-id")).ReturnsAsync(adminUser);
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(adminUser);

            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var result = await controller.DeleteUser("admin-user-id");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Users", redirect.ActionName);
            _userManager.Verify(m => m.DeleteAsync(It.IsAny<ApplicationUser>()), Times.Never);
        }

        [Fact]
        public async Task DeleteUser_DeletesUserAndRelatedData()
        {
            var context = TestDbContextFactory.CreateWithData();
            var targetUser = new ApplicationUser { Id = "test-user-id", UserName = "TestUser" };
            var adminUser = new ApplicationUser { Id = "admin-user-id", UserName = "Admin" };

            _userManager.Setup(m => m.FindByIdAsync("test-user-id")).ReturnsAsync(targetUser);
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(adminUser);
            _userManager.Setup(m => m.DeleteAsync(targetUser))
                .ReturnsAsync(IdentityResult.Success);

            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var bookingsBefore = await context.Bookings.CountAsync(b => b.UserId == "test-user-id");
            Assert.Equal(2, bookingsBefore);

            await controller.DeleteUser("test-user-id");

            var bookingsAfter = await context.Bookings.CountAsync(b => b.UserId == "test-user-id");
            Assert.Equal(0, bookingsAfter);
            _userManager.Verify(m => m.DeleteAsync(targetUser), Times.Once);
        }

        [Fact]
        public async Task SearchMoviesApi_EmptyQuery_ReturnsEmptyJson()
        {
            var context = TestDbContextFactory.Create();
            var controller = new AdminController(context, _userManager.Object, _roleManager.Object, _httpClientFactory.Object, _configuration);
            MockHelpers.SetupControllerContext(controller, "admin-user-id", "Admin", "Admin");

            var result = await controller.SearchMoviesApi("") as JsonResult;
            Assert.NotNull(result);
        }
    }
}
