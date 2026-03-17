using AryTickets.Controllers;
using AryTickets.Data;
using AryTickets.Models;
using AryTickets.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AryTickets.Tests.Controllers
{
    public class MovieControllerTests
    {
        private readonly Mock<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>> _userManager;

        public MovieControllerTests()
        {
            _userManager = MockHelpers.MockUserManager();
        }

        private (MovieController controller, Mock<HttpMessageHandler> handler) CreateController(ApplicationDbContext? context = null)
        {
            context ??= TestDbContextFactory.CreateWithData();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "TMDb:ApiKey", "test-key" }
                })
                .Build();

            var mockHandler = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(mockHandler.Object);
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var controller = new MovieController(factory.Object, config, context, _userManager.Object);
            MockHelpers.SetupControllerContext(controller, "test-user-id");
            return (controller, mockHandler);
        }

        [Fact]
        public async Task Details_ValidMovie_ReturnsViewWithMovie()
        {
            var context = TestDbContextFactory.CreateWithData();
            var (controller, handler) = CreateController(context);
            _userManager.Setup(m => m.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .Returns("test-user-id");

            var movieJson = "{\"id\":100,\"title\":\"Test Movie\",\"overview\":\"Overview\",\"poster_path\":\"/p.jpg\",\"backdrop_path\":\"/b.jpg\",\"release_date\":\"2026-01-01\",\"vote_average\":8.0,\"genre_ids\":[28],\"genres\":[{\"id\":28,\"name\":\"Action\"}],\"credits\":{\"cast\":[]},\"videos\":{\"results\":[]},\"reviews\":{\"results\":[]}}";

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(movieJson)
                });

            var result = await controller.Details(100) as ViewResult;

            Assert.NotNull(result);
            var movie = result!.Model as Movie;
            Assert.NotNull(movie);
            Assert.Equal("Test Movie", movie!.Title);
            Assert.True((bool)controller.ViewData["IsFavorite"]!);

            var userReviews = controller.ViewData["UserReviews"] as List<UserReview>;
            Assert.NotNull(userReviews);
            Assert.Equal(2, userReviews!.Count);
        }

        [Fact]
        public async Task Details_ApiReturnsError_ReturnsNotFound()
        {
            var (controller, handler) = CreateController();

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.NotFound));

            var result = await controller.Details(999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SubmitReview_ValidReview_RedirectsToDetails()
        {
            var context = TestDbContextFactory.Create();
            var user = new ApplicationUser
            {
                Id = "new-user-id",
                UserName = "NewUser",
                Email = "new@test.com",
                IsCritic = false
            };
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);

            var (controller, _) = CreateController(context);

            var result = await controller.SubmitReview(100, "Test Movie", 8, "Great!");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);
            Assert.Equal(100, redirect.RouteValues!["id"]);
            Assert.Single(await context.UserReviews.ToListAsync());
        }

        [Fact]
        public async Task SubmitReview_DuplicateReview_DoesNotAdd()
        {
            var context = TestDbContextFactory.CreateWithData();
            var user = new ApplicationUser
            {
                Id = "test-user-id",
                UserName = "TestUser",
                Email = "test@test.com"
            };
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);

            var (controller, _) = CreateController(context);

            // test-user-id already reviewed movie 100
            var result = await controller.SubmitReview(100, "Test Movie", 8, "Duplicate!");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Details", redirect.ActionName);

            var reviews = await context.UserReviews.CountAsync(r => r.UserId == "test-user-id" && r.MovieId == 100);
            Assert.Equal(1, reviews);
        }

        [Fact]
        public async Task SubmitReview_NotLoggedIn_RedirectsToLogin()
        {
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync((ApplicationUser)null!);

            var (controller, _) = CreateController();

            var result = await controller.SubmitReview(100, "Movie", 5, "Content");

            var redirect = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Login", redirect.ActionName);
        }

        [Fact]
        public async Task Search_EmptyQuery_ReturnsEmptyList()
        {
            var (controller, _) = CreateController();

            var result = await controller.Search("") as ViewResult;

            Assert.NotNull(result);
            var movies = result!.Model as List<Movie>;
            Assert.NotNull(movies);
            Assert.Empty(movies!);
        }

        [Fact]
        public async Task Search_ValidQuery_ReturnsResults()
        {
            var (controller, handler) = CreateController();

            var json = "{\"results\":[{\"id\":1,\"title\":\"Found Movie\",\"overview\":\"X\",\"poster_path\":\"/p.jpg\",\"backdrop_path\":\"/b.jpg\",\"release_date\":\"2026-01-01\",\"vote_average\":7.0,\"genre_ids\":[]}]}";

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                });

            var result = await controller.Search("Found") as ViewResult;

            var movies = result!.Model as List<Movie>;
            Assert.NotNull(movies);
            Assert.Single(movies!);
            Assert.Equal("Found", controller.ViewData["SearchQuery"]);
        }

        [Fact]
        public async Task Search_ApiError_ReturnsEmptyList()
        {
            var (controller, handler) = CreateController();

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var result = await controller.Search("test") as ViewResult;

            var movies = result!.Model as List<Movie>;
            Assert.Empty(movies!);
        }

        [Fact]
        public async Task Details_UnauthenticatedUser_SetsIsFavoriteFalse()
        {
            var context = TestDbContextFactory.CreateWithData();
            _userManager.Setup(m => m.GetUserId(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .Returns((string)null!);

            var (controller, handler) = CreateController(context);

            var movieJson = "{\"id\":100,\"title\":\"Test\",\"overview\":\"X\",\"poster_path\":\"/p.jpg\",\"backdrop_path\":\"/b.jpg\",\"release_date\":\"2026-01-01\",\"vote_average\":7.0,\"genre_ids\":[],\"genres\":[],\"credits\":{\"cast\":[]},\"videos\":{\"results\":[]},\"reviews\":{\"results\":[]}}";
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(movieJson)
                });

            var result = await controller.Details(100) as ViewResult;
            Assert.False((bool)controller.ViewData["IsFavorite"]!);
        }
    }
}
