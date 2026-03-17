using AryTickets.Controllers;
using AryTickets.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    public class HomeControllerTests
    {
        private readonly Mock<ILogger<HomeController>> _logger;

        public HomeControllerTests()
        {
            _logger = new Mock<ILogger<HomeController>>();
        }

        private (HomeController controller, Mock<HttpMessageHandler> handler) CreateControllerWithMockHttp(string? apiKey = "test-key")
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "TMDb:ApiKey", apiKey }
                })
                .Build();

            var mockHandler = new Mock<HttpMessageHandler>();
            var httpClient = new HttpClient(mockHandler.Object);
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var controller = new HomeController(_logger.Object, factory.Object, config);
            return (controller, mockHandler);
        }

        private void SetupMockResponse(Mock<HttpMessageHandler> handler, string url, string json, HttpStatusCode status = HttpStatusCode.OK)
        {
            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(m => m.RequestUri!.ToString().Contains(url)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(status)
                {
                    Content = new StringContent(json)
                });
        }

        [Fact]
        public async Task Index_NoApiKey_ReturnsEmptyViewModel()
        {
            var (controller, _) = CreateControllerWithMockHttp(apiKey: "");

            var result = await controller.Index() as ViewResult;

            Assert.NotNull(result);
            var model = result!.Model as HomeViewModel;
            Assert.NotNull(model);
            Assert.Empty(model!.NowShowingMovies);
            Assert.Empty(model.ComingSoonMovies);
        }

        [Fact]
        public async Task Index_WithApiKey_FetchesMovies()
        {
            var (controller, handler) = CreateControllerWithMockHttp();

            var genreJson = "{\"genres\":[{\"id\":28,\"name\":\"Action\"}]}";
            var moviesJson = "{\"results\":[{\"id\":1,\"title\":\"Test Movie\",\"overview\":\"A test\",\"poster_path\":\"/test.jpg\",\"backdrop_path\":\"/back.jpg\",\"release_date\":\"2026-01-01\",\"vote_average\":7.5,\"genre_ids\":[28]}]}";

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(moviesJson)
                });

            var result = await controller.Index() as ViewResult;
            Assert.NotNull(result);
            var model = result!.Model as HomeViewModel;
            Assert.NotNull(model);
        }

        [Fact]
        public async Task Index_WithGenreFilter_FiltersMovies()
        {
            var (controller, handler) = CreateControllerWithMockHttp();

            var moviesJson = "{\"results\":[{\"id\":1,\"title\":\"Action Movie\",\"overview\":\"A\",\"poster_path\":\"/1.jpg\",\"backdrop_path\":\"/1b.jpg\",\"release_date\":\"2026-01-01\",\"vote_average\":7.0,\"genre_ids\":[28]},{\"id\":2,\"title\":\"Comedy Movie\",\"overview\":\"B\",\"poster_path\":\"/2.jpg\",\"backdrop_path\":\"/2b.jpg\",\"release_date\":\"2026-01-01\",\"vote_average\":6.0,\"genre_ids\":[35]}]}";
            var genreJson = "{\"genres\":[{\"id\":28,\"name\":\"Action\"},{\"id\":35,\"name\":\"Comedy\"}]}";

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(moviesJson)
                });

            var result = await controller.Index(genreId: 28) as ViewResult;
            var model = result!.Model as HomeViewModel;
            Assert.NotNull(model);
            // Only action movies should remain in now showing
            Assert.All(model!.NowShowingMovies, m => Assert.Contains(28, m.GenreIds));
        }

        [Fact]
        public async Task Index_ApiReturnsError_ReturnsEmptyLists()
        {
            var (controller, handler) = CreateControllerWithMockHttp();

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var result = await controller.Index() as ViewResult;
            var model = result!.Model as HomeViewModel;
            Assert.NotNull(model);
            Assert.Empty(model!.NowShowingMovies);
        }

        [Fact]
        public async Task Index_SetsCurrentRegionInViewData()
        {
            var (controller, handler) = CreateControllerWithMockHttp(apiKey: "");

            await controller.Index("BG");

            Assert.Equal("BG", controller.ViewData["CurrentRegion"]);
        }

        [Fact]
        public async Task Index_RemovesDuplicatesBetweenNowShowingAndComingSoon()
        {
            var (controller, handler) = CreateControllerWithMockHttp();

            // Same movie in both now showing and coming soon
            var json = "{\"results\":[{\"id\":1,\"title\":\"Shared Movie\",\"overview\":\"X\",\"poster_path\":\"/1.jpg\",\"backdrop_path\":\"/1b.jpg\",\"release_date\":\"2026-01-01\",\"vote_average\":7.0,\"genre_ids\":[28]}]}";

            handler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                });

            var result = await controller.Index() as ViewResult;
            var model = result!.Model as HomeViewModel;

            // Movie in now showing should NOT appear in coming soon
            Assert.Empty(model!.ComingSoonMovies);
        }

        [Fact]
        public void Privacy_ReturnsView()
        {
            var (controller, _) = CreateControllerWithMockHttp(apiKey: "");
            var result = controller.Privacy();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public void StatusCode_404_ReturnsNotFoundView()
        {
            var (controller, _) = CreateControllerWithMockHttp(apiKey: "");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = controller.StatusCode(404) as ViewResult;
            Assert.NotNull(result);
            Assert.Equal("NotFound", result!.ViewName);
        }

        [Fact]
        public void StatusCode_403_ReturnsAccessDeniedView()
        {
            var (controller, _) = CreateControllerWithMockHttp(apiKey: "");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = controller.StatusCode(403) as ViewResult;
            Assert.NotNull(result);
            Assert.Equal("AccessDenied", result!.ViewName);
        }

        [Fact]
        public void StatusCode_500_ReturnsErrorView()
        {
            var (controller, _) = CreateControllerWithMockHttp(apiKey: "");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = controller.StatusCode(500) as ViewResult;
            Assert.NotNull(result);
            Assert.Equal("Error", result!.ViewName);
        }

        [Fact]
        public void Error_ReturnsViewWithRequestId()
        {
            var (controller, _) = CreateControllerWithMockHttp(apiKey: "");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            };

            var result = controller.Error() as ViewResult;
            Assert.NotNull(result);
            var model = result!.Model as ErrorViewModel;
            Assert.NotNull(model);
        }
    }
}
