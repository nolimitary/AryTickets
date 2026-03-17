using AryTickets.Controllers;
using AryTickets.Models;
using AryTickets.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AryTickets.Tests.Controllers
{
    public class UserFavoritesControllerTests
    {
        [Fact]
        public async Task ToggleFavorite_AddNew_CreatesUserFavorite()
        {
            var context = TestDbContextFactory.Create();
            var userManager = MockHelpers.MockUserManager();
            userManager.Setup(m => m.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .Returns("test-user-id");

            var controller = new UserFavoritesController(context, userManager.Object);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            var result = await controller.ToggleFavorite(999, "New Movie", "/poster.jpg");

            Assert.Single(await context.UserFavorites.ToListAsync());
            var fav = await context.UserFavorites.FirstAsync();
            Assert.Equal("test-user-id", fav.UserId);
            Assert.Equal(999, fav.MovieId);
            Assert.Equal("New Movie", fav.MovieTitle);
        }

        [Fact]
        public async Task ToggleFavorite_AlreadyExists_RemovesFavorite()
        {
            var context = TestDbContextFactory.CreateWithData();
            var userManager = MockHelpers.MockUserManager();
            userManager.Setup(m => m.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .Returns("test-user-id");

            var controller = new UserFavoritesController(context, userManager.Object);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            // Movie 100 is already in favorites for test-user-id
            var result = await controller.ToggleFavorite(100, "Favorite Movie", "/poster.jpg");

            var remaining = await context.UserFavorites
                .Where(f => f.UserId == "test-user-id" && f.MovieId == 100)
                .ToListAsync();
            Assert.Empty(remaining);
        }

        [Fact]
        public async Task ToggleFavorite_RedirectsToMovieDetails()
        {
            var context = TestDbContextFactory.Create();
            var userManager = MockHelpers.MockUserManager();
            userManager.Setup(m => m.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .Returns("test-user-id");

            var controller = new UserFavoritesController(context, userManager.Object);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            var result = await controller.ToggleFavorite(999, "Movie", "/poster.jpg");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal("Movie", redirect.ControllerName);
            Assert.Equal(999, redirect.RouteValues["id"]);
        }
    }
}
