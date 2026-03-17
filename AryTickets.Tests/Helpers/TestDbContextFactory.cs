using AryTickets.Data;
using AryTickets.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace AryTickets.Tests.Helpers
{
    public static class TestDbContextFactory
    {
        public static ApplicationDbContext Create()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        public static ApplicationDbContext CreateWithData()
        {
            var context = Create();

            // Seed test user
            var user = new ApplicationUser
            {
                Id = "test-user-id",
                UserName = "TestUser",
                Email = "test@test.com",
                EmailConfirmed = true,
                NormalizedEmail = "TEST@TEST.COM",
                NormalizedUserName = "TESTUSER"
            };
            context.Users.Add(user);

            // Seed admin user
            var admin = new ApplicationUser
            {
                Id = "admin-user-id",
                UserName = "Admin",
                Email = "admin@arytix.com",
                EmailConfirmed = true,
                NormalizedEmail = "ADMIN@ARYTIX.COM",
                NormalizedUserName = "ADMIN"
            };
            context.Users.Add(admin);

            // Seed critic user
            var critic = new ApplicationUser
            {
                Id = "critic-user-id",
                UserName = "CriticUser",
                Email = "critic@test.com",
                EmailConfirmed = true,
                IsCritic = true,
                NormalizedEmail = "CRITIC@TEST.COM",
                NormalizedUserName = "CRITICUSER"
            };
            context.Users.Add(critic);

            // Seed bookings
            context.Bookings.Add(new Booking
            {
                Id = 1,
                UserId = "test-user-id",
                UserEmail = "test@test.com",
                UserName = "TestUser",
                MovieTitle = "Test Movie 1",
                Showtime = "Mar 20, 2026 - 7:00 PM",
                Seats = "A1, A2",
                TotalPrice = 25.00m,
                BookedAt = DateTime.UtcNow.AddDays(-1),
                ConfirmationCode = "ABC12345"
            });

            context.Bookings.Add(new Booking
            {
                Id = 2,
                UserId = "test-user-id",
                UserEmail = "test@test.com",
                UserName = "TestUser",
                MovieTitle = "Test Movie 2",
                Showtime = "Mar 21, 2026 - 9:00 PM",
                Seats = "B3",
                TotalPrice = 12.50m,
                BookedAt = DateTime.UtcNow,
                ConfirmationCode = "DEF67890"
            });

            // Seed favorites
            context.UserFavorites.Add(new UserFavorite
            {
                Id = 1,
                UserId = "test-user-id",
                MovieId = 100,
                MovieTitle = "Favorite Movie",
                PosterPath = "/poster.jpg"
            });

            // Seed reviews
            context.UserReviews.Add(new UserReview
            {
                Id = 1,
                UserId = "test-user-id",
                UserName = "TestUser",
                MovieId = 100,
                MovieTitle = "Test Movie 1",
                Rating = 8,
                Content = "Great movie!",
                CreatedAt = DateTime.UtcNow
            });

            context.UserReviews.Add(new UserReview
            {
                Id = 2,
                UserId = "critic-user-id",
                UserName = "CriticUser",
                IsCritic = true,
                MovieId = 100,
                MovieTitle = "Test Movie 1",
                Rating = 9,
                Content = "Excellent cinematography",
                CreatedAt = DateTime.UtcNow
            });

            // Seed showtimes
            context.Showtimes.Add(new Showtime
            {
                Id = 1,
                TmdbMovieId = 100,
                MovieTitle = "Test Movie 1",
                PosterPath = "/poster1.jpg",
                ShowDateTime = DateTime.UtcNow.AddDays(1),
                Hall = "Hall 1",
                Price = 12.50m,
                IsActive = true
            });

            context.Showtimes.Add(new Showtime
            {
                Id = 2,
                TmdbMovieId = 100,
                MovieTitle = "Test Movie 1",
                PosterPath = "/poster1.jpg",
                ShowDateTime = DateTime.UtcNow.AddDays(-1),
                Hall = "Hall 2",
                Price = 15.00m,
                IsActive = true
            });

            context.Showtimes.Add(new Showtime
            {
                Id = 3,
                TmdbMovieId = 200,
                MovieTitle = "Inactive Movie",
                PosterPath = "/poster2.jpg",
                ShowDateTime = DateTime.UtcNow.AddDays(2),
                Hall = "Hall 1",
                Price = 12.50m,
                IsActive = false
            });

            // Seed seat reservations
            context.SeatReservations.Add(new SeatReservation
            {
                Id = 1,
                ShowtimeId = 1,
                BookingId = 1,
                SeatNumber = "A1"
            });

            // Seed critic application
            context.CriticApplications.Add(new CriticApplication
            {
                Id = 1,
                UserId = "test-user-id",
                FullName = "Test User",
                Bio = "I love movies",
                YearsOfExperience = 5,
                PastEmployers = "Movie Magazine",
                Motivation = "I want to share my opinions",
                Status = ApplicationStatus.Pending,
                AppliedAt = DateTime.UtcNow
            });

            context.SaveChanges();
            return context;
        }
    }
}
