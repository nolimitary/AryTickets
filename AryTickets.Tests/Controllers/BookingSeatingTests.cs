using AryTickets.Controllers;
using AryTickets.Models;
using AryTickets.Services;
using AryTickets.Tests.Helpers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AryTickets.Tests.Controllers
{
    public class BookingSeatingTests
    {
        private readonly Mock<IEmailSender> _emailSender = new();
        private readonly Mock<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>> _userManager = MockHelpers.MockUserManager();
        private readonly Mock<TicketPdfGenerator> _pdfGenerator = new();

        [Fact]
        public async Task SelectSeats_WithReservedSeats_ShowsCorrectAvailability()
        {
            var context = TestDbContextFactory.CreateWithData();
            var controller = new BookingController(_emailSender.Object, _userManager.Object, context, _pdfGenerator.Object);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            var result = await controller.SelectSeats(showtimeId: 1) as ViewResult;
            var model = result!.Model as SeatSelectionViewModel;

            // Verify seating chart is generated
            Assert.NotNull(model!.SeatingChart);
            Assert.Equal(8, model.SeatingChart.Count); // 8 rows A-H

            // Check that all non-null seats have proper seat numbers
            var allSeats = model.SeatingChart.SelectMany(r => r).Where(s => s != null).ToList();
            Assert.True(allSeats.Count > 0);

            // A1 should be taken (from test data)
            var a1 = allSeats.FirstOrDefault(s => s.SeatNumber == "A1");
            Assert.NotNull(a1);
            Assert.Equal(SeatStatus.Taken, a1!.Status);

            // A2 should be available
            var a2 = allSeats.FirstOrDefault(s => s.SeatNumber == "A2");
            Assert.NotNull(a2);
            Assert.Equal(SeatStatus.Available, a2!.Status);
        }

        [Fact]
        public async Task SelectSeats_SeatingChartHasCorrectStructure()
        {
            var context = TestDbContextFactory.CreateWithData();
            var controller = new BookingController(_emailSender.Object, _userManager.Object, context, _pdfGenerator.Object);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            var result = await controller.SelectSeats(showtimeId: 1) as ViewResult;
            var model = result!.Model as SeatSelectionViewModel;

            // Each row should have 14 positions (some null for aisles)
            foreach (var row in model!.SeatingChart)
            {
                Assert.Equal(14, row.Count);
            }

            // Row H should have null corners (positions 1-2 and 13-14)
            var rowH = model.SeatingChart[7];
            Assert.Null(rowH[0]);
            Assert.Null(rowH[1]);
            Assert.Null(rowH[12]);
            Assert.Null(rowH[13]);

            // Middle aisle (positions 7 and 8, indices 6 and 7) should be null
            foreach (var row in model.SeatingChart)
            {
                Assert.Null(row[6]);
                Assert.Null(row[7]);
            }
        }

        [Fact]
        public async Task SelectSeats_SetsTicketPrice()
        {
            var context = TestDbContextFactory.CreateWithData();
            var controller = new BookingController(_emailSender.Object, _userManager.Object, context, _pdfGenerator.Object);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            var result = await controller.SelectSeats(showtimeId: 1) as ViewResult;
            var model = result!.Model as SeatSelectionViewModel;

            Assert.Equal(12.50m, model!.TicketPrice);

            // All available seats should have the showtime price
            var seats = model.SeatingChart.SelectMany(r => r).Where(s => s != null && s.Status == SeatStatus.Available).ToList();
            Assert.All(seats, s => Assert.Equal(12.50m, s.Price));
        }

        [Fact]
        public async Task SelectSeats_FallbackMode_WithMovieInfo()
        {
            var context = TestDbContextFactory.Create();
            var controller = new BookingController(_emailSender.Object, _userManager.Object, context, _pdfGenerator.Object);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            var result = await controller.SelectSeats(null, 100, "Fallback Movie", "7:00 PM") as ViewResult;

            Assert.NotNull(result);
            var model = result!.Model as SeatSelectionViewModel;
            Assert.Equal("Fallback Movie", model!.MovieTitle);
            Assert.Equal("7:00 PM", model.Showtime);
            Assert.NotNull(model.SeatingChart);
        }

        [Fact]
        public async Task ProcessPayment_CreatesCorrectConfirmationCode()
        {
            var context = TestDbContextFactory.Create();
            var user = new ApplicationUser { Id = "u1", UserName = "Test", Email = "test@test.com" };
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);
            _emailSender.Setup(m => m.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var controller = new BookingController(_emailSender.Object, _userManager.Object, context, _pdfGenerator.Object);
            MockHelpers.SetupControllerContext(controller, "u1");

            var model = new CheckoutViewModel
            {
                MovieTitle = "Movie",
                Showtime = "7PM",
                SelectedSeats = "A1",
                TotalPrice = 12.50m,
                CardHolderName = "Test",
                CardNumber = "4111111111111111",
                ExpiryDate = "12/26",
                Cvc = "123"
            };

            var result = await controller.ProcessPayment(model) as JsonResult;
            Assert.NotNull(result);

            var booking = context.Bookings.First();
            Assert.NotNull(booking.ConfirmationCode);
            Assert.Equal(8, booking.ConfirmationCode.Length);
        }

        [Fact]
        public async Task ProcessPayment_WithPdfGenerator_SendsEmailWithAttachment()
        {
            var context = TestDbContextFactory.Create();
            var user = new ApplicationUser { Id = "u1", UserName = "Test", Email = "test@test.com" };
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);

            // Setup PDF generator to return fake PDF bytes
            var pdfGen = new Mock<TicketPdfGenerator>();
            pdfGen.Setup(p => p.Generate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new byte[] { 0x25, 0x50, 0x44, 0x46 });

            _emailSender.Setup(m => m.SendEmailWithAttachmentAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<byte[]>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var controller = new BookingController(_emailSender.Object, _userManager.Object, context, pdfGen.Object);
            MockHelpers.SetupControllerContext(controller, "u1");

            var model = new CheckoutViewModel
            {
                MovieTitle = "Movie With PDF",
                Showtime = "8PM",
                SelectedSeats = "B1, B2",
                TotalPrice = 25.00m,
                CardHolderName = "Test",
                CardNumber = "4111111111111111",
                ExpiryDate = "12/26",
                Cvc = "123"
            };

            var result = await controller.ProcessPayment(model) as JsonResult;
            Assert.NotNull(result);

            // Verify email was sent with attachment
            _emailSender.Verify(m => m.SendEmailWithAttachmentAsync(
                "test@test.com",
                It.Is<string>(s => s.Contains("Movie With PDF")),
                It.IsAny<string>(),
                It.IsAny<byte[]>(),
                It.Is<string>(s => s.EndsWith(".pdf"))
            ), Times.Once);
        }

        [Fact]
        public async Task ProcessPayment_PdfGeneratorFails_StillSendsEmailWithoutAttachment()
        {
            var context = TestDbContextFactory.Create();
            var user = new ApplicationUser { Id = "u1", UserName = "Test", Email = "test@test.com" };
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);

            // PDF generator throws
            var pdfGen = new Mock<TicketPdfGenerator>();
            pdfGen.Setup(p => p.Generate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new System.Exception("PDF generation failed"));

            _emailSender.Setup(m => m.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var controller = new BookingController(_emailSender.Object, _userManager.Object, context, pdfGen.Object);
            MockHelpers.SetupControllerContext(controller, "u1");

            var model = new CheckoutViewModel
            {
                MovieTitle = "Movie",
                Showtime = "8PM",
                SelectedSeats = "A1",
                TotalPrice = 12.50m,
                CardHolderName = "Test",
                CardNumber = "4111111111111111",
                ExpiryDate = "12/26",
                Cvc = "123"
            };

            var result = await controller.ProcessPayment(model) as JsonResult;
            Assert.NotNull(result);

            // Verify plain email was sent (not with attachment)
            _emailSender.Verify(m => m.SendEmailAsync(
                "test@test.com",
                It.IsAny<string>(),
                It.IsAny<string>()
            ), Times.Once);
        }

        [Fact]
        public async Task ProcessPayment_WithShowtimeId_CreatesSeatReservations()
        {
            var context = TestDbContextFactory.CreateWithData();
            var user = new ApplicationUser { Id = "u1", UserName = "Test", Email = "test@test.com" };
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);
            _emailSender.Setup(m => m.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var controller = new BookingController(_emailSender.Object, _userManager.Object, context, _pdfGenerator.Object);
            MockHelpers.SetupControllerContext(controller, "u1");

            var model = new CheckoutViewModel
            {
                MovieTitle = "Movie",
                Showtime = "8PM",
                SelectedSeats = "C1, C2, C3",
                TotalPrice = 37.50m,
                ShowtimeId = 1,
                CardHolderName = "Test",
                CardNumber = "4111111111111111",
                ExpiryDate = "12/26",
                Cvc = "123"
            };

            await controller.ProcessPayment(model);

            var booking = context.Bookings.OrderByDescending(b => b.Id).First();
            var reservations = context.SeatReservations.Where(r => r.BookingId == booking.Id).ToList();
            Assert.Equal(3, reservations.Count);
        }

        [Fact]
        public async Task ProcessPayment_WithoutShowtimeId_NoSeatReservations()
        {
            var context = TestDbContextFactory.Create();
            var user = new ApplicationUser { Id = "u1", UserName = "Test", Email = "test@test.com" };
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(user);
            _emailSender.Setup(m => m.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var controller = new BookingController(_emailSender.Object, _userManager.Object, context, _pdfGenerator.Object);
            MockHelpers.SetupControllerContext(controller, "u1");

            var model = new CheckoutViewModel
            {
                MovieTitle = "Movie",
                Showtime = "8PM",
                SelectedSeats = "A1",
                TotalPrice = 12.50m,
                ShowtimeId = null, // No showtime ID
                CardHolderName = "Test",
                CardNumber = "4111111111111111",
                ExpiryDate = "12/26",
                Cvc = "123"
            };

            await controller.ProcessPayment(model);

            Assert.Empty(context.SeatReservations.ToList());
        }

        [Fact]
        public void Checkout_SetsShowtimeId()
        {
            var context = TestDbContextFactory.Create();
            var controller = new BookingController(_emailSender.Object, _userManager.Object, context, _pdfGenerator.Object);
            MockHelpers.SetupControllerContext(controller, "test-user-id");

            var result = controller.Checkout("Movie", "7PM", "A1", 12.50m, 42) as ViewResult;
            var model = result!.Model as CheckoutViewModel;

            Assert.Equal(42, model!.ShowtimeId);
            Assert.Equal("7PM", model.Showtime);
        }
    }
}
