using AryTickets.Controllers;
using AryTickets.Models;
using AryTickets.Tests.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace AryTickets.Tests.Controllers
{
    public class AdminAnalyticsTests
    {
        private AdminController CreateController(AryTickets.Data.ApplicationDbContext context)
        {
            var userManager = MockHelpers.MockUserManager();
            var roleManager = MockHelpers.MockRoleManager();
            var httpFactory = new Mock<IHttpClientFactory>();
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "TMDb:ApiKey", "test" }
                }).Build();

            userManager.Setup(m => m.Users).Returns(context.Users);

            var controller = new AdminController(context, userManager.Object, roleManager.Object, httpFactory.Object, config);
            MockHelpers.SetupControllerContext(controller, "admin-id", "Admin", "Admin");
            return controller;
        }

        [Fact]
        public async Task Analytics_EmptyBookings_ReturnsZeroValues()
        {
            var context = TestDbContextFactory.Create();
            var controller = CreateController(context);

            var result = await controller.Analytics() as ViewResult;
            var model = result!.Model as AnalyticsViewModel;

            Assert.NotNull(model);
            Assert.Equal(0m, model!.TotalRevenue);
            Assert.Equal(0, model.TotalBookings);
            Assert.Equal(0m, model.AverageOrderValue);
            Assert.Equal(0m, model.TodayRevenue);
        }

        [Fact]
        public async Task Analytics_WithBookings_CalculatesCorrectRevenue()
        {
            var context = TestDbContextFactory.CreateWithData();
            var controller = CreateController(context);

            var result = await controller.Analytics() as ViewResult;
            var model = result!.Model as AnalyticsViewModel;

            Assert.Equal(37.50m, model!.TotalRevenue);
            Assert.Equal(2, model.TotalBookings);
        }

        [Fact]
        public async Task Analytics_RevenueDates_Has30Entries()
        {
            var context = TestDbContextFactory.CreateWithData();
            var controller = CreateController(context);

            var result = await controller.Analytics() as ViewResult;
            var model = result!.Model as AnalyticsViewModel;

            Assert.Equal(30, model!.RevenueDates.Count);
            Assert.Equal(30, model.RevenueValues.Count);
        }

        [Fact]
        public async Task Analytics_DayNames_HasAllWeekDays()
        {
            var context = TestDbContextFactory.CreateWithData();
            var controller = CreateController(context);

            var result = await controller.Analytics() as ViewResult;
            var model = result!.Model as AnalyticsViewModel;

            Assert.Equal(7, model!.DayNames.Count);
            Assert.Contains("Mon", model.DayNames);
            Assert.Contains("Sun", model.DayNames);
        }

        [Fact]
        public async Task Analytics_TopMovies_OrderedByBookingCount()
        {
            var context = TestDbContextFactory.CreateWithData();
            var controller = CreateController(context);

            var result = await controller.Analytics() as ViewResult;
            var model = result!.Model as AnalyticsViewModel;

            Assert.NotEmpty(model!.TopMovieNames);
            Assert.NotEmpty(model.TopMovieBookings);
        }

        [Fact]
        public async Task Analytics_TodayRevenue_OnlyCountsToday()
        {
            var context = TestDbContextFactory.Create();
            // Add a booking from today
            context.Bookings.Add(new Booking
            {
                UserId = "u1",
                UserEmail = "u@test.com",
                UserName = "U1",
                MovieTitle = "Today Movie",
                Showtime = "Now",
                Seats = "A1",
                TotalPrice = 50.00m,
                BookedAt = DateTime.UtcNow,
                ConfirmationCode = "TODAY1"
            });
            // Add a booking from yesterday
            context.Bookings.Add(new Booking
            {
                UserId = "u1",
                UserEmail = "u@test.com",
                UserName = "U1",
                MovieTitle = "Yesterday Movie",
                Showtime = "Yesterday",
                Seats = "B1",
                TotalPrice = 30.00m,
                BookedAt = DateTime.UtcNow.AddDays(-1),
                ConfirmationCode = "YEST1"
            });
            await context.SaveChangesAsync();

            var controller = CreateController(context);
            var result = await controller.Analytics() as ViewResult;
            var model = result!.Model as AnalyticsViewModel;

            Assert.Equal(50.00m, model!.TodayRevenue);
            Assert.Equal(80.00m, model.TotalRevenue);
        }

        [Fact]
        public async Task Index_Dashboard_ShowsCorrectKPIs()
        {
            var context = TestDbContextFactory.CreateWithData();
            var controller = CreateController(context);

            var result = await controller.Index() as ViewResult;

            Assert.NotNull(result);
            var totalBookings = (int)controller.ViewData["TotalBookings"]!;
            var totalFavorites = (int)controller.ViewData["TotalFavorites"]!;
            Assert.Equal(2, totalBookings);
            Assert.Equal(1, totalFavorites);
        }
    }
}
