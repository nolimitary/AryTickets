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
    public class ProductionControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly Mock<UserManager<ApplicationUser>> _userManager;

        public ProductionControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);
            _userManager = MockHelpers.MockUserManager();
        }

        public void Dispose() => _db.Dispose();

        private ProductionController CreateController(string? userId = null)
        {
            _userManager.Setup(m => m.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>())).Returns(userId);
            if (userId != null)
            {
                _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                    .ReturnsAsync(new ApplicationUser { Id = userId, UserName = "Tester", IsCritic = false });
            }

            var controller = new ProductionController(_db, _userManager.Object);
            MockHelpers.SetupControllerContext(controller, userId ?? "anon");
            return controller;
        }

        [Fact]
        public async Task Details_NotFound_ReturnsNotFound()
        {
            var controller = CreateController();
            var result = await controller.Details(9999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_InactiveProduction_ReturnsNotFound()
        {
            _db.Productions.Add(new Production { Title = "X", IsActive = false });
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.Details(1);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ActiveProduction_ReturnsViewWithProduction()
        {
            var p = new Production { Title = "Хамлет", IsActive = true };
            _db.Productions.Add(p);
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.Details(p.Id);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(p, view.Model);
            Assert.False((bool)controller.ViewData["IsFavorite"]!);
        }

        [Fact]
        public async Task Details_FavoritedByUser_FlagsFavorite()
        {
            var p = new Production { Title = "Хамлет", IsActive = true };
            _db.Productions.Add(p);
            await _db.SaveChangesAsync();

            _db.UserFavorites.Add(new UserFavorite
            {
                UserId = "user-1",
                ProductionId = p.Id,
                ProductionTitle = p.Title,
                PosterUrl = ""
            });
            await _db.SaveChangesAsync();

            var controller = CreateController(userId: "user-1");
            var result = await controller.Details(p.Id);

            var view = Assert.IsType<ViewResult>(result);
            Assert.True((bool)controller.ViewData["IsFavorite"]!);
        }

        [Fact]
        public async Task Details_PopulatesPerformancesAndReviews()
        {
            var p = new Production { Title = "Хамлет", IsActive = true };
            _db.Productions.Add(p);
            await _db.SaveChangesAsync();

            _db.Performances.Add(new Performance
            {
                ProductionId = p.Id,
                IsActive = true,
                ShowDateTime = DateTime.UtcNow.AddDays(1),
                Price = 30m
            });
            _db.UserReviews.Add(new UserReview
            {
                ProductionId = p.Id,
                UserId = "x",
                UserName = "Bob",
                ProductionTitle = p.Title,
                Rating = 5,
                Content = "Great",
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            var controller = CreateController();
            await controller.Details(p.Id);

            var perfs = Assert.IsAssignableFrom<List<Performance>>(controller.ViewData["Performances"]);
            Assert.Single(perfs);

            var reviews = Assert.IsAssignableFrom<List<UserReview>>(controller.ViewData["UserReviews"]);
            Assert.Single(reviews);
        }

        [Fact]
        public async Task SubmitReview_UnauthenticatedUser_RedirectsToLogin()
        {
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync((ApplicationUser?)null);
            var controller = new ProductionController(_db, _userManager.Object);
            MockHelpers.SetupControllerContext(controller, "x");

            var result = await controller.SubmitReview(1, "X", 5, "good");
            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
        }

        [Fact]
        public async Task SubmitReview_NewReview_PersistsAndRedirects()
        {
            var controller = CreateController(userId: "user-1");
            var result = await controller.SubmitReview(7, "X", 4, "fine");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Single(_db.UserReviews);
        }

        [Fact]
        public async Task SubmitReview_DuplicateReview_DoesNotInsertSecond()
        {
            _db.UserReviews.Add(new UserReview
            {
                UserId = "user-1",
                UserName = "Tester",
                ProductionId = 7,
                ProductionTitle = "X",
                Rating = 5,
                Content = "first",
                CreatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();

            var controller = CreateController(userId: "user-1");
            await controller.SubmitReview(7, "X", 1, "second");

            Assert.Single(_db.UserReviews);
        }

        [Fact]
        public async Task Search_EmptyQuery_ReturnsEmptyList()
        {
            var controller = CreateController();
            var result = await controller.Search("");

            var view = Assert.IsType<ViewResult>(result);
            var list = Assert.IsAssignableFrom<List<Production>>(view.Model);
            Assert.Empty(list);
        }

        [Fact]
        public async Task Search_MatchesTitle()
        {
            _db.Productions.AddRange(
                new Production { Title = "Хамлет", IsActive = true },
                new Production { Title = "Отело", IsActive = true }
            );
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.Search("Хам");
            var view = Assert.IsType<ViewResult>(result);
            var list = Assert.IsAssignableFrom<List<Production>>(view.Model);

            Assert.Single(list);
            Assert.Equal("Хамлет", list[0].Title);
        }

        [Fact]
        public async Task Search_IgnoresInactiveResults()
        {
            _db.Productions.AddRange(
                new Production { Title = "X-Active", IsActive = true },
                new Production { Title = "X-Inactive", IsActive = false }
            );
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.Search("X-");
            var view = Assert.IsType<ViewResult>(result);
            var list = Assert.IsAssignableFrom<List<Production>>(view.Model);

            Assert.Single(list);
            Assert.Equal("X-Active", list[0].Title);
        }
    }
}
