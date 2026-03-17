using AryTickets.Controllers;
using AryTickets.Models;
using AryTickets.Services;
using AryTickets.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AryTickets.Tests.Controllers
{
    public class BookingControllerTests
    {
        private readonly Mock<IEmailSender> _emailSender;
        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly Mock<TicketPdfGenerator> _pdfGenerator;

        public BookingControllerTests()
        {
            _emailSender = new Mock<IEmailSender>();
            _userManager = MockHelpers.MockUserManager();
            _pdfGenerator = new Mock<TicketPdfGenerator>();
        }

        [Fact]
        public async Task SelectSeats_ValidShowtimeId_ReturnsView()
        {
            var context = TestDbContextFactory.CreateWithData();
            var controller = new BookingController(_emailSender.Object, _userManager.Object, context, _pdfGenerator.Object);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            var result = await controller.SelectSeats(showtimeId: 1) as ViewResult;

            Assert.NotNull(result);
            var model = result.Model as SeatSelectionViewModel;
            Assert.NotNull(model);
            Assert.Equal("Test Movie 1", model.MovieTitle);
            Assert.Equal("Hall 1", model.Hall);
            Assert.Equal(12.50m, model.TicketPrice);
        }

        [Fact]
        public async Task SelectSeats_InvalidShowtimeId_ReturnsNotFound()
        {
            var context = TestDbContextFactory.Create();
            var controller = new BookingController(_emailSender.Object, _userManager.Object, context, _pdfGenerator.Object);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            var result = await controller.SelectSeats(showtimeId: 999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task SelectSeats_NoShowtimeId_NoMovieTitle_ReturnsBadRequest()
        {
            var context = TestDbContextFactory.Create();
            var controller = new BookingController(_emailSender.Object, _userManager.Object, context, _pdfGenerator.Object);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            var result = await controller.SelectSeats(null, 0, null, null);
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SelectSeats_ShowsReservedSeats()
        {
            var context = TestDbContextFactory.CreateWithData();
            var controller = new BookingController(_emailSender.Object, _userManager.Object, context, _pdfGenerator.Object);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            var result = await controller.SelectSeats(showtimeId: 1) as ViewResult;
            var model = result.Model as SeatSelectionViewModel;

            // Seat A1 is reserved in test data
            var allSeats = model.SeatingChart.SelectMany(r => r).Where(s => s != null).ToList();
            var seatA1 = allSeats.FirstOrDefault(s => s.SeatNumber == "A1");
            Assert.NotNull(seatA1);
            Assert.Equal(SeatStatus.Taken, seatA1.Status);
        }

        [Fact]
        public void Checkout_ReturnsViewWithModel()
        {
            var context = TestDbContextFactory.Create();
            var controller = new BookingController(_emailSender.Object, _userManager.Object, context, _pdfGenerator.Object);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            var result = controller.Checkout("Test Movie", "7:00 PM", "A1, A2", 25.00m, 1);

            var viewResult = Assert.IsType<ViewResult>(result);
            var model = viewResult.Model as CheckoutViewModel;
            Assert.NotNull(model);
            Assert.Equal("Test Movie", model.MovieTitle);
            Assert.Equal("A1, A2", model.SelectedSeats);
            Assert.Equal(25.00m, model.TotalPrice);
        }

        [Fact]
        public async Task ProcessPayment_ValidPayment_CreatesBookingAndReservations()
        {
            var context = TestDbContextFactory.CreateWithData();
            var user = new ApplicationUser
            {
                Id = "test-user-id",
                UserName = "TestUser",
                Email = "test@test.com",
                NormalizedEmail = "TEST@TEST.COM"
            };
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);
            _emailSender.Setup(m => m.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            _emailSender.Setup(m => m.SendEmailWithAttachmentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var controller = new BookingController(_emailSender.Object, _userManager.Object, context, _pdfGenerator.Object);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            var model = new CheckoutViewModel
            {
                MovieTitle = "New Booking Movie",
                Showtime = "Mar 25 - 8:00 PM",
                SelectedSeats = "C1, C2",
                TotalPrice = 25.00m,
                ShowtimeId = 1,
                CardHolderName = "Test User",
                CardNumber = "4111111111111111",
                ExpiryDate = "12/26",
                Cvc = "123"
            };

            var result = await controller.ProcessPayment(model);

            var jsonResult = Assert.IsType<JsonResult>(result);
            var bookings = await context.Bookings.Where(b => b.MovieTitle == "New Booking Movie").ToListAsync();
            Assert.Single(bookings);
            Assert.Equal("test-user-id", bookings[0].UserId);

            // Check seat reservations were created
            var reservations = await context.SeatReservations
                .Where(r => r.ShowtimeId == 1 && r.BookingId == bookings[0].Id)
                .ToListAsync();
            Assert.Equal(2, reservations.Count);
        }

        [Fact]
        public async Task ProcessPayment_NullUser_StillReturnsSuccess()
        {
            var context = TestDbContextFactory.Create();
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync((ApplicationUser)null);

            var controller = new BookingController(_emailSender.Object, _userManager.Object, context, _pdfGenerator.Object);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            var model = new CheckoutViewModel
            {
                MovieTitle = "Movie",
                Showtime = "7 PM",
                SelectedSeats = "A1",
                TotalPrice = 12.50m,
                CardHolderName = "Test",
                CardNumber = "4111111111111111",
                ExpiryDate = "12/26",
                Cvc = "123"
            };

            var result = await controller.ProcessPayment(model);
            var jsonResult = Assert.IsType<JsonResult>(result);
        }
    }
}
