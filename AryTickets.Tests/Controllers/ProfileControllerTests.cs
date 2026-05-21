using AryTickets.Controllers;
using AryTickets.Data;
using AryTickets.Models;
using AryTickets.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AryTickets.Tests.Controllers
{
    public class ProfileControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManager;

        public ProfileControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);
            _userManager = MockHelpers.MockUserManager();
            _signInManager = MockHelpers.MockSignInManager(_userManager);
        }

        public void Dispose() => _db.Dispose();

        private ProfileController CreateController(ApplicationUser? user = null)
        {
            user ??= new ApplicationUser
            {
                Id = "user-1",
                UserName = "Tester",
                Email = "t@x.com",
                IsCritic = false,
                CreatedAt = DateTime.UtcNow.AddMonths(-3)
            };
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync(user);
            _userManager.Setup(m => m.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns(user.Id);

            var controller = new ProfileController(_userManager.Object, _signInManager.Object, _db);
            MockHelpers.SetupControllerContext(controller, user.Id);
            return controller;
        }

        // ═══ Index ═══

        [Fact]
        public async Task Index_NullUser_RedirectsToLogin()
        {
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync((ApplicationUser?)null);
            var controller = new ProfileController(_userManager.Object, _signInManager.Object, _db);
            MockHelpers.SetupControllerContext(controller, "x");

            var result = await controller.Index();
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
        }

        [Fact]
        public async Task Index_PopulatesStats()
        {
            _db.Bookings.AddRange(
                new Booking { UserId = "user-1", ProductionTitle = "X", TotalPrice = 30m, BookedAt = DateTime.UtcNow },
                new Booking { UserId = "user-1", ProductionTitle = "Y", TotalPrice = 40m, BookedAt = DateTime.UtcNow }
            );
            _db.UserFavorites.Add(new UserFavorite { UserId = "user-1", ProductionId = 1, ProductionTitle = "X", PosterUrl = "" });
            _db.UserReviews.Add(new UserReview { UserId = "user-1", UserName = "Tester", ProductionId = 1, ProductionTitle = "X", Content = "x", Rating = 5, CreatedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.Index();

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<ProfileViewModel>(view.Model);
            Assert.Equal(2, model.TicketCount);
            Assert.Equal(70m, model.TotalSpent);
            Assert.Equal(1, model.FavoritesCount);
            Assert.Equal(1, model.ReviewsCount);
        }

        // ═══ CriticApplication ═══

        [Fact]
        public async Task CriticApplication_Get_AlreadyCritic_RedirectsHome()
        {
            var critic = new ApplicationUser { Id = "user-1", UserName = "Critic", Email = "c@x.com", IsCritic = true, CreatedAt = DateTime.UtcNow };
            var controller = CreateController(critic);

            var result = await controller.CriticApplication();
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
        }

        [Fact]
        public async Task CriticApplication_Get_ExistingApplication_ShowsStatusView()
        {
            _db.CriticApplications.Add(new CriticApplication
            {
                UserId = "user-1",
                FullName = "Anon",
                Bio = "bio",
                PastEmployers = "none",
                Motivation = "want",
                Status = ApplicationStatus.Pending,
                AppliedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.CriticApplication();
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal("CriticApplicationStatus", view.ViewName);
        }

        [Fact]
        public async Task CriticApplication_Get_NoApplication_ShowsForm()
        {
            var controller = CreateController();
            var result = await controller.CriticApplication();
            var view = Assert.IsType<ViewResult>(result);
            Assert.IsType<CriticApplicationViewModel>(view.Model);
        }

        [Fact]
        public async Task CriticApplication_Post_Valid_PersistsApplication()
        {
            var controller = CreateController();
            var model = new CriticApplicationViewModel
            {
                FullName = "John Doe",
                Bio = "Theatre critic for 5 years",
                YearsOfExperience = 5,
                PastEmployers = "Daily Theatre",
                Motivation = "Love theatre",
                ReviewLinks = new List<string> { "http://example.com/review1" }
            };

            var result = await controller.CriticApplication(model);
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirect.ActionName);
            Assert.Single(_db.CriticApplications);
        }

        [Fact]
        public async Task CriticApplication_Post_InvalidModel_ReturnsView()
        {
            var controller = CreateController();
            controller.ModelState.AddModelError("FullName", "Required");
            var result = await controller.CriticApplication(new CriticApplicationViewModel());
            Assert.IsType<ViewResult>(result);
            Assert.Empty(_db.CriticApplications);
        }

        // ═══ BookingHistory / Favorites / Settings ═══

        [Fact]
        public async Task BookingHistory_ReturnsOnlyCurrentUserBookings()
        {
            _db.Bookings.AddRange(
                new Booking { UserId = "user-1", ProductionTitle = "Mine", TotalPrice = 10, BookedAt = DateTime.UtcNow },
                new Booking { UserId = "user-2", ProductionTitle = "Other", TotalPrice = 10, BookedAt = DateTime.UtcNow }
            );
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var view = Assert.IsType<ViewResult>(await controller.BookingHistory());
            var list = Assert.IsAssignableFrom<List<Booking>>(view.Model);
            Assert.Single(list);
            Assert.Equal("Mine", list[0].ProductionTitle);
        }

        [Fact]
        public async Task Favorites_ReturnsCurrentUserFavorites()
        {
            _db.UserFavorites.AddRange(
                new UserFavorite { UserId = "user-1", ProductionId = 1, ProductionTitle = "X", PosterUrl = "" },
                new UserFavorite { UserId = "user-2", ProductionId = 1, ProductionTitle = "X", PosterUrl = "" }
            );
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var view = Assert.IsType<ViewResult>(await controller.Favorites());
            var list = Assert.IsAssignableFrom<List<UserFavorite>>(view.Model);
            Assert.Single(list);
        }

        [Fact]
        public async Task Reviews_ReturnsView()
        {
            var controller = CreateController();
            var result = await controller.Reviews();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Settings_Get_ReturnsViewWithCurrentUserData()
        {
            var controller = CreateController();
            var view = Assert.IsType<ViewResult>(await controller.Settings());
            var model = Assert.IsType<SettingsViewModel>(view.Model);
            Assert.Equal("Tester", model.Username);
            Assert.Equal("t@x.com", model.Email);
        }

        [Fact]
        public async Task Settings_Get_NullUser_ReturnsNotFound()
        {
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync((ApplicationUser?)null);
            var controller = new ProfileController(_userManager.Object, _signInManager.Object, _db);
            MockHelpers.SetupControllerContext(controller, "x");

            Assert.IsType<NotFoundResult>(await controller.Settings());
        }

        // ═══ UpdateProfile ═══

        [Fact]
        public async Task UpdateProfile_NullUser_ReturnsNotFound()
        {
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync((ApplicationUser?)null);
            var controller = new ProfileController(_userManager.Object, _signInManager.Object, _db);
            MockHelpers.SetupControllerContext(controller, "x");

            var result = await controller.UpdateProfile(new SettingsViewModel());
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task UpdateProfile_UsernameChange_CallsSetUserName()
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "Old", Email = "t@x.com" };
            _userManager.Setup(m => m.SetUserNameAsync(user, "New")).ReturnsAsync(IdentityResult.Success);
            var controller = CreateController(user);

            var model = new SettingsViewModel { Username = "New", Email = "t@x.com" };
            var result = await controller.UpdateProfile(model);

            _userManager.Verify(m => m.SetUserNameAsync(user, "New"), Times.Once);
            Assert.IsType<RedirectToActionResult>(result);
        }

        [Fact]
        public async Task UpdateProfile_EmailChange_CallsSetEmail()
        {
            var user = new ApplicationUser { Id = "user-1", UserName = "u", Email = "old@x.com" };
            _userManager.Setup(m => m.SetEmailAsync(user, "new@x.com")).ReturnsAsync(IdentityResult.Success);
            var controller = CreateController(user);

            var model = new SettingsViewModel { Username = "u", Email = "new@x.com" };
            var result = await controller.UpdateProfile(model);

            _userManager.Verify(m => m.SetEmailAsync(user, "new@x.com"), Times.Once);
            Assert.IsType<RedirectToActionResult>(result);
        }

        // ═══ ChangePassword ═══

        [Fact]
        public async Task ChangePassword_NullUser_ReturnsNotFound()
        {
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync((ApplicationUser?)null);
            var controller = new ProfileController(_userManager.Object, _signInManager.Object, _db);
            MockHelpers.SetupControllerContext(controller, "x");

            var result = await controller.ChangePassword(new SettingsViewModel());
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ChangePassword_Success_RedirectsAndRefreshes()
        {
            var user = new ApplicationUser { Id = "user-1" };
            _userManager.Setup(m => m.ChangePasswordAsync(user, "Old1!", "New1!")).ReturnsAsync(IdentityResult.Success);
            var controller = CreateController(user);

            var model = new SettingsViewModel { OldPassword = "Old1!", NewPassword = "New1!" };
            var result = await controller.ChangePassword(model);

            Assert.IsType<RedirectToActionResult>(result);
            _signInManager.Verify(m => m.RefreshSignInAsync(user), Times.Once);
        }

        [Fact]
        public async Task ChangePassword_Failure_SetsErrorMessage()
        {
            var user = new ApplicationUser { Id = "user-1" };
            _userManager.Setup(m => m.ChangePasswordAsync(user, "Old1!", "New1!"))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "bad password" }));
            var controller = CreateController(user);

            var model = new SettingsViewModel { OldPassword = "Old1!", NewPassword = "New1!" };
            await controller.ChangePassword(model);

            Assert.NotNull(controller.TempData["ErrorMessage"]);
        }

        // ═══ DeleteAccount ═══

        [Fact]
        public async Task DeleteAccount_Success_SignsOutAndRedirects()
        {
            var user = new ApplicationUser { Id = "user-1" };
            _userManager.Setup(m => m.DeleteAsync(user)).ReturnsAsync(IdentityResult.Success);
            var controller = CreateController(user);

            var result = await controller.DeleteAccount();
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Home", redirect.ControllerName);
            _signInManager.Verify(m => m.SignOutAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAccount_Failure_SetsErrorMessage()
        {
            var user = new ApplicationUser { Id = "user-1" };
            _userManager.Setup(m => m.DeleteAsync(user))
                .ReturnsAsync(IdentityResult.Failed(new IdentityError { Description = "fail" }));
            var controller = CreateController(user);

            await controller.DeleteAccount();
            Assert.NotNull(controller.TempData["ErrorMessage"]);
        }

        [Fact]
        public async Task DeleteAccount_NullUser_ReturnsNotFound()
        {
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).ReturnsAsync((ApplicationUser?)null);
            var controller = new ProfileController(_userManager.Object, _signInManager.Object, _db);
            MockHelpers.SetupControllerContext(controller, "x");

            Assert.IsType<NotFoundResult>(await controller.DeleteAccount());
        }
    }
}
