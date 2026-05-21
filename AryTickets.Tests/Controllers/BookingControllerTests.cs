using AryTickets.Controllers;
using AryTickets.Data;
using AryTickets.Hubs;
using AryTickets.Models;
using AryTickets.Services;
using AryTickets.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AryTickets.Tests.Controllers
{
    public class BookingControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly Mock<IEmailSender> _emailSender;
        private readonly Mock<IHubContext<SeatHub>> _seatHub;
        private readonly Mock<TicketPdfGenerator> _pdfGenerator;
        private readonly StripeSettings _stripeSettings;

        public BookingControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);

            _userManager = MockHelpers.MockUserManager();
            _emailSender = new Mock<IEmailSender>();
            _seatHub = MockHelpers.MockSeatHub();
            _pdfGenerator = new Mock<TicketPdfGenerator>();
            _pdfGenerator.Setup(p => p.Generate(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<decimal>(),
                It.IsAny<string>(),
                It.IsAny<byte[]>()
            )).Returns(new byte[] { 0x25, 0x50, 0x44, 0x46 });

            _stripeSettings = new StripeSettings();
        }

        public void Dispose() => _db.Dispose();

        private BookingController CreateController(string userId = "user-1", string email = "u@test.com")
        {
            var controller = new BookingController(
                _emailSender.Object,
                _userManager.Object,
                _db,
                _pdfGenerator.Object,
                _seatHub.Object,
                _stripeSettings);
            MockHelpers.SetupControllerContext(controller, userId);

            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { Id = userId, UserName = "Test", Email = email });
            return controller;
        }

        private async Task<Performance> SeedUpcomingPerformanceAsync()
        {
            var production = new Production { Title = "Хамлет", Genre = "Трагедия", IsActive = true };
            _db.Productions.Add(production);
            await _db.SaveChangesAsync();

            var perf = new Performance
            {
                ProductionId = production.Id,
                ShowDateTime = DateTime.UtcNow.AddDays(2),
                Stage = "Голяма сцена",
                Price = 35m,
                IsActive = true
            };
            _db.Performances.Add(perf);
            await _db.SaveChangesAsync();
            return perf;
        }

        // ═══ SelectSeats ═══

        [Fact]
        public async Task SelectSeats_NoPerformanceId_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.SelectSeats(null);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SelectSeats_UnknownPerformance_ReturnsNotFound()
        {
            var controller = CreateController();
            var result = await controller.SelectSeats(9999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SelectSeats_PastPerformance_ReturnsBadRequest()
        {
            var production = new Production { Title = "X", IsActive = true };
            _db.Productions.Add(production);
            await _db.SaveChangesAsync();

            var perf = new Performance
            {
                ProductionId = production.Id,
                ShowDateTime = DateTime.UtcNow.AddDays(-1),
                IsActive = true,
                Price = 20m,
            };
            _db.Performances.Add(perf);
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.SelectSeats(perf.Id);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SelectSeats_Valid_ReturnsViewWithSeatingChart()
        {
            var perf = await SeedUpcomingPerformanceAsync();
            var controller = CreateController();

            var result = await controller.SelectSeats(perf.Id);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<SeatSelectionViewModel>(view.Model);
            Assert.Equal(perf.Id, model.PerformanceId);
            Assert.Equal("Хамлет", model.ProductionTitle);
            Assert.NotEmpty(model.SeatingChart);
        }

        [Fact]
        public async Task SelectSeats_MarksReservedSeatsAsTaken()
        {
            var perf = await SeedUpcomingPerformanceAsync();
            _db.SeatReservations.Add(new SeatReservation
            {
                PerformanceId = perf.Id,
                SeatNumber = "A1"
            });
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var view = (ViewResult)await controller.SelectSeats(perf.Id);
            var model = (SeatSelectionViewModel)view.Model!;

            var allSeats = model.SeatingChart.SelectMany(r => r).Where(s => s != null);
            var a1 = allSeats.First(s => s.SeatNumber == "A1");
            Assert.Equal(SeatStatus.Taken, a1.Status);
        }

        // ═══ Checkout ═══

        [Fact]
        public void Checkout_ReturnsView_WithStripeFlags()
        {
            var controller = CreateController();
            var result = controller.Checkout("Хамлет", "12 май · 19:00", "Голяма сцена", "A1,A2", 70m, 1);

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<CheckoutViewModel>(view.Model);
            Assert.Equal("Хамлет", model.ProductionTitle);
            Assert.Equal(70m, model.TotalPrice);
            Assert.Equal(false, controller.ViewData["StripeEnabled"]);
        }

        [Fact]
        public void Checkout_StripeConfigured_FlagTrue()
        {
            _stripeSettings.SecretKey = "sk_test_x";
            _stripeSettings.PublishableKey = "pk_test_x";

            var controller = CreateController();
            var result = controller.Checkout("X", "now", "Stage", "A1", 30m, 1);

            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(true, controller.ViewData["StripeEnabled"]);
            Assert.Equal("pk_test_x", controller.ViewData["StripePublishableKey"]);
        }

        // ═══ StripeConfig ═══

        [Fact]
        public void StripeConfig_ReturnsConfigJson()
        {
            _stripeSettings.PublishableKey = "pk_test_abc";
            _stripeSettings.SecretKey = "sk_test_abc";

            var controller = CreateController();
            var result = controller.StripeConfig();

            var json = Assert.IsType<JsonResult>(result);
            var data = json.Value!;
            var prop = data.GetType().GetProperty("publishableKey");
            Assert.NotNull(prop);
            Assert.Equal("pk_test_abc", prop!.GetValue(data));
        }

        // ═══ CreatePaymentIntent ═══

        [Fact]
        public async Task CreatePaymentIntent_StripeUnconfigured_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.CreatePaymentIntent(30m, 1, "A1");
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreatePaymentIntent_MissingPerformanceId_ReturnsBadRequest()
        {
            _stripeSettings.SecretKey = "sk_test_x";
            _stripeSettings.PublishableKey = "pk_test_x";
            var controller = CreateController();
            var result = await controller.CreatePaymentIntent(0m, null, "A1");
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task CreatePaymentIntent_UnknownPerformance_ReturnsNotFound()
        {
            _stripeSettings.SecretKey = "sk_test_x";
            _stripeSettings.PublishableKey = "pk_test_x";
            var controller = CreateController();
            var result = await controller.CreatePaymentIntent(0m, 9999, "A1");
            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task CreatePaymentIntent_TakenSeats_ReturnsConflict()
        {
            _stripeSettings.SecretKey = "sk_test_x";
            _stripeSettings.PublishableKey = "pk_test_x";

            var perf = await SeedUpcomingPerformanceAsync();
            _db.SeatReservations.Add(new SeatReservation
            {
                PerformanceId = perf.Id,
                SeatNumber = "A1"
            });
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.CreatePaymentIntent(30m, perf.Id, "A1,A2");

            Assert.IsType<ConflictObjectResult>(result);
        }

        // ═══ ProcessPayment (Stripe-required) ═══

        [Fact]
        public async Task ProcessPayment_NoStripeConfig_ReturnsFailureJson()
        {
            var perf = await SeedUpcomingPerformanceAsync();
            var controller = CreateController();
            // _stripeSettings.IsConfigured is false in this fixture.
            var model = new CheckoutViewModel
            {
                ProductionTitle = "Хамлет",
                PerformanceDateTime = "now",
                SelectedSeats = "A1",
                TotalPrice = 35m,
                PerformanceId = perf.Id,
                StripePaymentIntentId = "pi_test_anything"
            };
            var result = await controller.ProcessPayment(model);
            var json = Assert.IsType<JsonResult>(result);
            var prop = json.Value!.GetType().GetProperty("success");
            Assert.False((bool)prop!.GetValue(json.Value)!);
            Assert.Empty(_db.Bookings);
        }

        [Fact]
        public async Task ProcessPayment_StripeConfiguredButMissingIntent_ReturnsFailureJson()
        {
            _stripeSettings.SecretKey = "sk_test_x";
            _stripeSettings.PublishableKey = "pk_test_x";
            var perf = await SeedUpcomingPerformanceAsync();
            var controller = CreateController();
            var model = new CheckoutViewModel
            {
                ProductionTitle = "Хамлет",
                PerformanceDateTime = "now",
                SelectedSeats = "A1",
                TotalPrice = 35m,
                PerformanceId = perf.Id,
                StripePaymentIntentId = null
            };
            var result = await controller.ProcessPayment(model);
            var json = Assert.IsType<JsonResult>(result);
            var prop = json.Value!.GetType().GetProperty("success");
            Assert.False((bool)prop!.GetValue(json.Value)!);
            Assert.Empty(_db.Bookings);
        }
    }
}
