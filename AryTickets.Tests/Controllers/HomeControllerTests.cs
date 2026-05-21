using AryTickets.Controllers;
using AryTickets.Data;
using AryTickets.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AryTickets.Tests.Controllers
{
    public class HomeControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _db;

        public HomeControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);
        }

        public void Dispose() => _db.Dispose();

        private HomeController CreateController()
        {
            var logger = Mock.Of<ILogger<HomeController>>();
            return new HomeController(logger, _db);
        }

        [Fact]
        public async Task Index_NoData_ReturnsEmptyRepertoire()
        {
            var controller = CreateController();
            var result = await controller.Index();
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<HomeViewModel>(view.Model);
            Assert.Empty(model.CurrentRepertoire);
            Assert.Empty(model.UpcomingPremieres);
        }

        [Fact]
        public async Task Index_ProductionWithUpcomingPerformance_AppearsInRepertoire()
        {
            var production = new Production
            {
                Title = "Хамлет",
                Genre = "Трагедия",
                IsActive = true,
                Rating = 9.0
            };
            _db.Productions.Add(production);
            await _db.SaveChangesAsync();

            _db.Performances.Add(new Performance
            {
                ProductionId = production.Id,
                ShowDateTime = DateTime.UtcNow.AddDays(3),
                IsActive = true,
                Price = 30m,
                Stage = "Голяма сцена"
            });
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.Index();
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<HomeViewModel>(view.Model);

            Assert.Single(model.CurrentRepertoire);
            Assert.Equal("Хамлет", model.CurrentRepertoire[0].Title);
        }

        [Fact]
        public async Task Index_FilterByGenre_OnlyMatchesShown()
        {
            _db.Productions.AddRange(
                new Production { Title = "A", Genre = "Трагедия", IsActive = true },
                new Production { Title = "B", Genre = "Комедия", IsActive = true }
            );
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.Index("Комедия");
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<HomeViewModel>(view.Model);

            Assert.Equal("Комедия", model.SelectedGenre);
            Assert.DoesNotContain(model.CurrentRepertoire.Concat(model.UpcomingPremieres), p => p.Genre == "Трагедия");
        }

        [Fact]
        public async Task Index_FuturePremiereWithoutPerformance_AppearsInUpcoming()
        {
            _db.Productions.Add(new Production
            {
                Title = "Премиера",
                Genre = "Драма",
                IsActive = true,
                PremiereDate = DateTime.UtcNow.AddDays(60)
            });
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.Index();
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<HomeViewModel>(view.Model);

            Assert.Single(model.UpcomingPremieres);
            Assert.Empty(model.CurrentRepertoire);
        }

        [Fact]
        public async Task Index_OnlyActiveProductionsSurfaced()
        {
            _db.Productions.AddRange(
                new Production { Title = "Active", Genre = "Драма", IsActive = true },
                new Production { Title = "Inactive", Genre = "Драма", IsActive = false }
            );
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.Index();
            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<HomeViewModel>(view.Model);

            var allTitles = model.CurrentRepertoire.Concat(model.UpcomingPremieres).Select(p => p.Title);
            Assert.DoesNotContain("Inactive", allTitles);
        }

        [Fact]
        public void Privacy_ReturnsView()
        {
            var controller = CreateController();
            var result = controller.Privacy();
            Assert.IsType<ViewResult>(result);
        }

        [Theory]
        [InlineData(403, "AccessDenied")]
        [InlineData(404, "NotFound")]
        [InlineData(500, "Error")]
        public void StatusCode_RoutesToNamedView(int code, string expectedView)
        {
            var controller = CreateController();
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
            };
            var result = controller.StatusCode(code);
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(expectedView, view.ViewName);
        }
    }
}
