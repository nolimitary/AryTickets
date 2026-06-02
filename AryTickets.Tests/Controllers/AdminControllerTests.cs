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
    public class AdminControllerTests : IDisposable
    {
        private readonly ApplicationDbContext _db;
        private readonly Mock<UserManager<ApplicationUser>> _userManager;
        private readonly Mock<RoleManager<IdentityRole>> _roleManager;

        public AdminControllerTests()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _db = new ApplicationDbContext(options);
            _userManager = MockHelpers.MockUserManager();
            _roleManager = MockHelpers.MockRoleManager();
        }

        public void Dispose() => _db.Dispose();

        private static CriticApplication MakeApp(string userId, ApplicationStatus status) => new()
        {
            UserId = userId,
            User = new ApplicationUser { Id = userId, UserName = userId, Email = userId + "@x.com" },
            FullName = "Anon",
            Bio = "bio",
            PastEmployers = "none",
            Motivation = "want",
            Status = status,
            AppliedAt = DateTime.UtcNow
        };

        private AdminController CreateController(string adminId = "admin-1")
        {
            var controller = new AdminController(_db, _userManager.Object, _roleManager.Object);
            MockHelpers.SetupControllerContext(controller, adminId, role: "Admin");
            _userManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { Id = adminId, UserName = "Admin", Email = "admin@x.com" });
            return controller;
        }

        // ═══ Bookings ═══

        [Fact]
        public async Task Bookings_ReturnsAllOrderedByDate()
        {
            _db.Bookings.AddRange(
                new Booking { Id = 1, UserId = "u1", ProductionTitle = "A", BookedAt = DateTime.UtcNow.AddDays(-2), TotalPrice = 10 },
                new Booking { Id = 2, UserId = "u1", ProductionTitle = "B", BookedAt = DateTime.UtcNow, TotalPrice = 20 }
            );
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.Bookings();

            var view = Assert.IsType<ViewResult>(result);
            var list = Assert.IsAssignableFrom<List<Booking>>(view.Model);
            Assert.Equal(2, list.Count);
            Assert.Equal("B", list[0].ProductionTitle);
        }

        [Fact]
        public async Task DeleteBooking_RemovesBookingAndReservations()
        {
            var booking = new Booking { Id = 1, UserId = "u1", ProductionTitle = "X", TotalPrice = 10, BookedAt = DateTime.UtcNow };
            _db.Bookings.Add(booking);
            _db.SeatReservations.Add(new SeatReservation { BookingId = 1, PerformanceId = 1, SeatNumber = "A1" });
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.DeleteBooking(1);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Empty(_db.Bookings);
            Assert.Empty(_db.SeatReservations);
        }

        [Fact]
        public async Task DeleteBooking_UnknownId_NoOp()
        {
            var controller = CreateController();
            var result = await controller.DeleteBooking(9999);
            Assert.IsType<RedirectToActionResult>(result);
        }

        // ═══ Applications ═══

        [Fact]
        public async Task Applications_PendingFilter_ReturnsOnlyPending()
        {
            _db.CriticApplications.AddRange(
                MakeApp("u1", ApplicationStatus.Pending),
                MakeApp("u2", ApplicationStatus.Approved)
            );
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.Applications("pending");

            var view = Assert.IsType<ViewResult>(result);
            var list = Assert.IsAssignableFrom<List<CriticApplication>>(view.Model);
            Assert.Single(list);
        }

        [Fact]
        public async Task Applications_ApprovedFilter_ReturnsApproved()
        {
            _db.CriticApplications.AddRange(
                MakeApp("u1", ApplicationStatus.Pending),
                MakeApp("u2", ApplicationStatus.Approved)
            );
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.Applications("approved");

            var view = Assert.IsType<ViewResult>(result);
            var list = Assert.IsAssignableFrom<List<CriticApplication>>(view.Model);
            Assert.Single(list);
            Assert.Equal(ApplicationStatus.Approved, list[0].Status);
        }

        [Fact]
        public async Task Applications_DeniedFilter_ReturnsDenied()
        {
            _db.CriticApplications.Add(MakeApp("u1", ApplicationStatus.Denied));
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.Applications("denied");

            var view = Assert.IsType<ViewResult>(result);
            var list = Assert.IsAssignableFrom<List<CriticApplication>>(view.Model);
            Assert.Single(list);
            Assert.Equal("denied", controller.ViewData["CurrentFilter"]);
        }

        [Fact]
        public async Task ApproveApplication_UnknownId_ReturnsNotFound()
        {
            var controller = CreateController();
            var result = await controller.ApproveApplication(9999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task ApproveApplication_Valid_SetsCriticFlagAndStatus()
        {
            var app = MakeApp("user-1", ApplicationStatus.Pending);
            _db.CriticApplications.Add(app);
            await _db.SaveChangesAsync();

            var user = new ApplicationUser { Id = "user-1", IsCritic = false };
            _userManager.Setup(m => m.FindByIdAsync("user-1")).ReturnsAsync(user);

            var controller = CreateController();
            var result = await controller.ApproveApplication(app.Id);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(ApplicationStatus.Approved, app.Status);
            Assert.True(user.IsCritic);
        }

        [Fact]
        public async Task DenyApplication_Valid_SetsStatusDenied()
        {
            var app = MakeApp("user-1", ApplicationStatus.Pending);
            _db.CriticApplications.Add(app);
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.DenyApplication(app.Id);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(ApplicationStatus.Denied, app.Status);
        }

        [Fact]
        public async Task DenyApplication_UnknownId_ReturnsNotFound()
        {
            var controller = CreateController();
            var result = await controller.DenyApplication(9999);
            Assert.IsType<NotFoundResult>(result);
        }

        // ═══ Productions ═══

        [Fact]
        public async Task Productions_ReturnsOnlyActive()
        {
            _db.Productions.AddRange(
                new Production { Title = "Active", IsActive = true },
                new Production { Title = "Inactive", IsActive = false }
            );
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.Productions();

            var view = Assert.IsType<ViewResult>(result);
            var list = Assert.IsAssignableFrom<List<Production>>(view.Model);
            Assert.Single(list);
            Assert.Equal("Active", list[0].Title);
        }

        [Fact]
        public void CreateProduction_Get_ReturnsView()
        {
            var controller = CreateController();
            var result = controller.CreateProduction();
            var view = Assert.IsType<ViewResult>(result);
            Assert.IsType<Production>(view.Model);
        }

        [Fact]
        public async Task CreateProduction_Post_Valid_PersistsAndRedirects()
        {
            var controller = CreateController();
            var model = new Production { Title = "New Play", Genre = "Драма" };

            var result = await controller.CreateProduction(model);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Single(_db.Productions);
            Assert.True(_db.Productions.First().IsActive);
        }

        [Fact]
        public async Task CreateProduction_Post_InvalidModel_ReturnsView()
        {
            var controller = CreateController();
            controller.ModelState.AddModelError("Title", "Required");

            var result = await controller.CreateProduction(new Production());

            Assert.IsType<ViewResult>(result);
            Assert.Empty(_db.Productions);
        }

        [Fact]
        public async Task EditProduction_Get_UnknownId_ReturnsNotFound()
        {
            var controller = CreateController();
            var result = await controller.EditProduction(9999);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task EditProduction_Get_ReturnsViewWithProduction()
        {
            var p = new Production { Title = "X", IsActive = true };
            _db.Productions.Add(p);
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.EditProduction(p.Id);
            var view = Assert.IsType<ViewResult>(result);
            Assert.Equal(p, view.Model);
        }

        [Fact]
        public async Task EditProduction_Post_UpdatesFields()
        {
            var p = new Production { Title = "Old", Synopsis = "S1", Genre = "G", IsActive = true, Director = "D1" };
            _db.Productions.Add(p);
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var updated = new Production { Id = p.Id, Title = "New", Synopsis = "S2", Genre = "G2", Director = "D2" };
            var result = await controller.EditProduction(updated);

            Assert.IsType<RedirectToActionResult>(result);
            var refreshed = _db.Productions.First();
            Assert.Equal("New", refreshed.Title);
            Assert.Equal("D2", refreshed.Director);
        }

        [Fact]
        public async Task DeleteProduction_SoftDeletes()
        {
            var p = new Production { Title = "X", IsActive = true };
            _db.Productions.Add(p);
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.DeleteProduction(p.Id);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.False(_db.Productions.First().IsActive);
        }

        // ═══ Performances ═══

        [Fact]
        public async Task Performances_ReturnsActiveOrdered()
        {
            var p = new Production { Title = "X", IsActive = true };
            _db.Productions.Add(p);
            await _db.SaveChangesAsync();
            _db.Performances.AddRange(
                new Performance { ProductionId = p.Id, IsActive = true, ShowDateTime = DateTime.UtcNow.AddDays(5), Price = 30 },
                new Performance { ProductionId = p.Id, IsActive = false, ShowDateTime = DateTime.UtcNow.AddDays(3), Price = 30 }
            );
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.Performances();
            var view = Assert.IsType<ViewResult>(result);
            var list = Assert.IsAssignableFrom<List<Performance>>(view.Model);
            Assert.Single(list);
        }

        [Fact]
        public async Task CreatePerformance_BadInputs_ReturnsBadRequest()
        {
            var controller = CreateController();
            var result = await controller.CreatePerformance(0, "", "Stage", 0m);
            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task CreatePerformance_UnknownProduction_ReturnsNotFound()
        {
            var controller = CreateController();
            var result = await controller.CreatePerformance(9999, "2030-01-01T19:00", "Stage", 30m);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task CreatePerformance_PersistsMultipleDates()
        {
            var p = new Production { Title = "X", IsActive = true };
            _db.Productions.Add(p);
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.CreatePerformance(p.Id, "2030-01-01T19:00, 2030-01-02T20:00", "Сцена", 40m);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal(2, _db.Performances.Count());
        }

        [Fact]
        public async Task DeletePerformance_SoftDeletes()
        {
            var perf = new Performance { ProductionId = 1, IsActive = true, ShowDateTime = DateTime.UtcNow.AddDays(1), Price = 30 };
            _db.Performances.Add(perf);
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var result = await controller.DeletePerformance(perf.Id);

            Assert.IsType<RedirectToActionResult>(result);
            Assert.False(_db.Performances.First().IsActive);
        }

        // ═══ Analytics ═══

        [Fact]
        public async Task Analytics_EmptyData_ReturnsModelWithZeroes()
        {
            var controller = CreateController();
            var result = await controller.Analytics();

            var view = Assert.IsType<ViewResult>(result);
            var model = Assert.IsType<AnalyticsViewModel>(view.Model);
            Assert.Equal(0m, model.TotalRevenue);
            Assert.Equal(0, model.TotalBookings);
        }

        [Fact]
        public async Task Analytics_AggregatesBookings()
        {
            _db.Bookings.AddRange(
                new Booking { UserId = "u1", ProductionTitle = "A", BookedAt = DateTime.UtcNow, TotalPrice = 50m },
                new Booking { UserId = "u1", ProductionTitle = "A", BookedAt = DateTime.UtcNow, TotalPrice = 30m },
                new Booking { UserId = "u2", ProductionTitle = "B", BookedAt = DateTime.UtcNow, TotalPrice = 100m }
            );
            await _db.SaveChangesAsync();

            var controller = CreateController();
            var view = Assert.IsType<ViewResult>(await controller.Analytics());
            var model = Assert.IsType<AnalyticsViewModel>(view.Model);

            Assert.Equal(3, model.TotalBookings);
            Assert.Equal(180m, model.TotalRevenue);
            Assert.Equal(60m, model.AverageOrderValue);
            Assert.Equal(7, model.DayCounts.Count);
        }
    }
}
