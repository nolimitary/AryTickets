using AryTickets.Data;
using AryTickets.Models;
using AryTickets.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace AryTickets.Tests.Data
{
    public class DbContextTests
    {
        [Fact]
        public async Task Bookings_CanCreateAndRetrieve()
        {
            var context = TestDbContextFactory.Create();

            context.Bookings.Add(new Booking
            {
                UserId = "user-1",
                UserEmail = "user@test.com",
                UserName = "User1",
                MovieTitle = "Test Movie",
                Showtime = "7 PM",
                Seats = "A1",
                TotalPrice = 12.50m,
                ConfirmationCode = "CODE123"
            });
            await context.SaveChangesAsync();

            var booking = await context.Bookings.FirstAsync();
            Assert.Equal("Test Movie", booking.MovieTitle);
            Assert.Equal(12.50m, booking.TotalPrice);
        }

        [Fact]
        public async Task UserFavorites_CanCreateAndDelete()
        {
            var context = TestDbContextFactory.Create();

            var fav = new UserFavorite
            {
                UserId = "user-1",
                MovieId = 100,
                MovieTitle = "Favorite",
                PosterPath = "/poster.jpg"
            };
            context.UserFavorites.Add(fav);
            await context.SaveChangesAsync();

            Assert.Single(await context.UserFavorites.ToListAsync());

            context.UserFavorites.Remove(fav);
            await context.SaveChangesAsync();

            Assert.Empty(await context.UserFavorites.ToListAsync());
        }

        [Fact]
        public async Task UserReviews_CanFilterByMovieId()
        {
            var context = TestDbContextFactory.CreateWithData();

            var reviews = await context.UserReviews
                .Where(r => r.MovieId == 100)
                .ToListAsync();

            Assert.Equal(2, reviews.Count);
        }

        [Fact]
        public async Task UserReviews_CanFilterByUserId()
        {
            var context = TestDbContextFactory.CreateWithData();

            var reviews = await context.UserReviews
                .Where(r => r.UserId == "test-user-id")
                .ToListAsync();

            Assert.Single(reviews);
        }

        [Fact]
        public async Task Showtimes_ActiveOnly_ExcludesInactive()
        {
            var context = TestDbContextFactory.CreateWithData();

            var active = await context.Showtimes
                .Where(s => s.IsActive)
                .ToListAsync();

            Assert.Equal(2, active.Count);
            Assert.DoesNotContain(active, s => s.MovieTitle == "Inactive Movie");
        }

        [Fact]
        public async Task Showtimes_FutureOnly_ExcludesPast()
        {
            var context = TestDbContextFactory.CreateWithData();

            var future = await context.Showtimes
                .Where(s => s.IsActive && s.ShowDateTime > DateTime.UtcNow)
                .ToListAsync();

            Assert.Single(future);
        }

        [Fact]
        public async Task SeatReservations_LinkedToShowtimeAndBooking()
        {
            var context = TestDbContextFactory.CreateWithData();

            var reservation = await context.SeatReservations.FirstAsync();
            Assert.Equal(1, reservation.ShowtimeId);
            Assert.Equal(1, reservation.BookingId);
            Assert.Equal("A1", reservation.SeatNumber);
        }

        [Fact]
        public async Task CriticApplications_CanUpdateStatus()
        {
            var context = TestDbContextFactory.CreateWithData();

            var app = await context.CriticApplications.FirstAsync();
            Assert.Equal(ApplicationStatus.Pending, app.Status);

            app.Status = ApplicationStatus.Approved;
            app.ReviewedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();

            var updated = await context.CriticApplications.FirstAsync();
            Assert.Equal(ApplicationStatus.Approved, updated.Status);
            Assert.NotNull(updated.ReviewedAt);
        }

        [Fact]
        public async Task Bookings_CanCalculateUserTotalSpent()
        {
            var context = TestDbContextFactory.CreateWithData();

            var total = await context.Bookings
                .Where(b => b.UserId == "test-user-id")
                .SumAsync(b => b.TotalPrice);

            Assert.Equal(37.50m, total);
        }

        [Fact]
        public async Task Bookings_OrderByBookedAtDescending_ReturnsNewestFirst()
        {
            var context = TestDbContextFactory.CreateWithData();

            var bookings = await context.Bookings
                .OrderByDescending(b => b.BookedAt)
                .ToListAsync();

            Assert.True(bookings[0].BookedAt >= bookings[1].BookedAt);
        }

        [Fact]
        public async Task CascadeDelete_RemovingBooking_AllowsSeatReservationRemoval()
        {
            var context = TestDbContextFactory.CreateWithData();

            var reservations = context.SeatReservations.Where(r => r.BookingId == 1);
            context.SeatReservations.RemoveRange(reservations);
            var booking = await context.Bookings.FindAsync(1);
            context.Bookings.Remove(booking);
            await context.SaveChangesAsync();

            Assert.Empty(await context.SeatReservations.Where(r => r.BookingId == 1).ToListAsync());
            Assert.Null(await context.Bookings.FindAsync(1));
        }

        [Fact]
        public async Task DbContext_HasAllRequiredDbSets()
        {
            var context = TestDbContextFactory.Create();

            Assert.NotNull(context.UserFavorites);
            Assert.NotNull(context.Bookings);
            Assert.NotNull(context.CriticApplications);
            Assert.NotNull(context.UserReviews);
            Assert.NotNull(context.Showtimes);
            Assert.NotNull(context.SeatReservations);
        }
    }
}
