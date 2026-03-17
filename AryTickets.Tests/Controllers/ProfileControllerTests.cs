using AryTickets.Controllers;
using AryTickets.Models;
using AryTickets.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Threading.Tasks;
using Xunit;

namespace AryTickets.Tests.Controllers
{
    public class ProfileControllerTests
    {
        private readonly Mock<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>> _userManager;
        private readonly Mock<Microsoft.AspNetCore.Identity.SignInManager<ApplicationUser>> _signInManager;

        public ProfileControllerTests()
        {
            _userManager = MockHelpers.MockUserManager();
            _signInManager = MockHelpers.MockSignInManager(_userManager);
        }

        [Fact]
        public async Task Index_AuthenticatedUser_ReturnsViewWithCorrectCounts()
        {
            var context = TestDbContextFactory.CreateWithData();
            var testUser = new ApplicationUser
            {
                Id = "test-user-id",
                UserName = "TestUser",
                Email = "test@test.com",
                EmailConfirmed = true
            };

            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(testUser);
            _userManager.Setup(m => m.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .Returns("test-user-id");

            var controller = new ProfileController(_userManager.Object, _signInManager.Object, context);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            var result = await controller.Index() as ViewResult;

            Assert.NotNull(result);
            var model = result.Model as ProfileViewModel;
            Assert.NotNull(model);
            Assert.Equal(2, model.TicketCount);
            Assert.Equal(1, model.FavoritesCount);
            Assert.Equal(1, model.ReviewsCount);
            Assert.Equal(37.50m, model.TotalSpent);
        }

        [Fact]
        public async Task Index_NullUser_RedirectsToLogin()
        {
            var context = TestDbContextFactory.Create();
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync((ApplicationUser)null);

            var controller = new ProfileController(_userManager.Object, _signInManager.Object, context);
            MockHelpers.SetupControllerContext(controller, "nonexistent");

            var result = await controller.Index();

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
        }

        [Fact]
        public async Task BookingHistory_ReturnsBookingsForUser()
        {
            var context = TestDbContextFactory.CreateWithData();
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { Id = "test-user-id", UserName = "TestUser", Email = "test@test.com" });
            _userManager.Setup(m => m.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .Returns("test-user-id");

            var controller = new ProfileController(_userManager.Object, _signInManager.Object, context);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            var result = await controller.BookingHistory() as ViewResult;

            Assert.NotNull(result);
            var bookings = result.Model as System.Collections.Generic.List<Booking>;
            Assert.NotNull(bookings);
            Assert.Equal(2, bookings.Count);
        }

        [Fact]
        public async Task Favorites_ReturnsUserFavorites()
        {
            var context = TestDbContextFactory.CreateWithData();
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { Id = "test-user-id", UserName = "TestUser", Email = "test@test.com" });
            _userManager.Setup(m => m.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .Returns("test-user-id");

            var controller = new ProfileController(_userManager.Object, _signInManager.Object, context);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            var result = await controller.Favorites() as ViewResult;

            Assert.NotNull(result);
            var favorites = result.Model as System.Collections.Generic.List<UserFavorite>;
            Assert.NotNull(favorites);
            Assert.Single(favorites);
        }

        [Fact]
        public async Task Settings_ReturnsViewWithUserData()
        {
            var testUser = new ApplicationUser
            {
                Id = "test-user-id",
                UserName = "TestUser",
                Email = "test@test.com"
            };
            var context = TestDbContextFactory.Create();
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(testUser);

            var controller = new ProfileController(_userManager.Object, _signInManager.Object, context);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            var result = await controller.Settings() as ViewResult;

            Assert.NotNull(result);
            var model = result.Model as SettingsViewModel;
            Assert.NotNull(model);
            Assert.Equal("TestUser", model.Username);
            Assert.Equal("test@test.com", model.Email);
        }

        [Fact]
        public async Task Settings_NullUser_ReturnsNotFound()
        {
            var context = TestDbContextFactory.Create();
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync((ApplicationUser)null);

            var controller = new ProfileController(_userManager.Object, _signInManager.Object, context);
            MockHelpers.SetupControllerContext(controller, "nonexistent");

            var result = await controller.Settings();
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CriticApplication_AlreadyCritic_RedirectsToIndex()
        {
            var criticUser = new ApplicationUser
            {
                Id = "critic-user-id",
                UserName = "CriticUser",
                Email = "critic@test.com",
                IsCritic = true
            };
            var context = TestDbContextFactory.Create();
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(criticUser);

            var controller = new ProfileController(_userManager.Object, _signInManager.Object, context);
            MockHelpers.SetupControllerContext(controller, "critic-user-id");

            var result = await controller.CriticApplication();
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task CriticApplication_ExistingApplication_ShowsStatus()
        {
            var context = TestDbContextFactory.CreateWithData();
            var testUser = new ApplicationUser
            {
                Id = "test-user-id",
                UserName = "TestUser",
                Email = "test@test.com",
                IsCritic = false
            };
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(testUser);

            var controller = new ProfileController(_userManager.Object, _signInManager.Object, context);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            var result = await controller.CriticApplication() as ViewResult;
            Assert.NotNull(result);
            Assert.Equal("CriticApplicationStatus", result.ViewName);
        }

        [Fact]
        public async Task DeleteAccount_ValidUser_DeletesAndRedirects()
        {
            var testUser = new ApplicationUser { Id = "test-user-id", UserName = "TestUser" };
            var context = TestDbContextFactory.Create();
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(testUser);
            _userManager.Setup(m => m.DeleteAsync(testUser))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.IdentityResult.Success);

            var controller = new ProfileController(_userManager.Object, _signInManager.Object, context);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            var result = await controller.DeleteAccount();
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Equal("Home", redirect.ControllerName);
        }
    }
}
