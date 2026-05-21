using AryTickets.Controllers;
using AryTickets.Data;
using AryTickets.Models;
using AryTickets.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AryTickets.Tests.Controllers
{
    public class UserFavoritesControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly Mock<UserManager<ApplicationUser>> _userManager;

        public UserFavoritesControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);
            _userManager = MockHelpers.MockUserManager();
            _userManager.Setup(m => m.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .Returns("user-1");
        }

        public void Dispose() => _db.Dispose();

        private UserFavoritesController CreateController()
        {
            var controller = new UserFavoritesController(_db, _userManager.Object);
            MockHelpers.SetupControllerContext(controller, "user-1");
            return controller;
        }

        [Fact]
        public async Task ToggleFavorite_AddsWhenAbsent()
        {
            var controller = CreateController();
            var result = await controller.ToggleFavorite(7, "Хамлет", "poster.jpg");

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Single(_db.UserFavorites);
            var fav = _db.UserFavorites.First();
            Assert.Equal("user-1", fav.UserId);
            Assert.Equal(7, fav.ProductionId);
            Assert.Equal("Хамлет", fav.ProductionTitle);
        }

        [Fact]
        public async Task ToggleFavorite_RemovesWhenPresent()
        {
            _db.UserFavorites.Add(new UserFavorite
            {
                UserId = "user-1",
                ProductionId = 7,
                ProductionTitle = "Хамлет",
                PosterUrl = "poster.jpg"
            });
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.ToggleFavorite(7, "Хамлет", "poster.jpg");

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Empty(_db.UserFavorites);
        }

        [Fact]
        public async Task ToggleFavorite_RedirectsToProductionDetails()
        {
            var controller = CreateController();
            var result = await controller.ToggleFavorite(42, "Title", "url");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal("Production", redirect.ControllerName);
            Assert.Equal(42, redirect.RouteValues!["id"]);
        }

        [Fact]
        public async Task ToggleFavorite_DifferentUsersIndependent()
        {
            _db.UserFavorites.Add(new UserFavorite
            {
                UserId = "user-2",
                ProductionId = 7,
                ProductionTitle = "Хамлет",
                PosterUrl = "p.jpg"
            });
            await _db.SaveChangesAsync();

            var controller = CreateController();
            await controller.ToggleFavorite(7, "Хамлет", "p.jpg");

            // user-1 added their own; user-2's favorite is untouched
            Assert.Equal(2, _db.UserFavorites.Count());
        }
    }
}
