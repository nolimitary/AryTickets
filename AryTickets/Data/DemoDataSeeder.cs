using AryTickets.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace AryTickets.Data
{
    // One-shot demo population: realistic users, bookings, reviews, favourites
    // and critic applications. Only runs when the only existing user is the
    // bootstrap admin, so it's safe to leave in startup forever.
    public static class DemoDataSeeder
    {
        private record DemoUser(
            string UserName, string Email, string FullName,
            bool IsCritic, int Bookings, int Favorites, int Reviews);

        private static readonly DemoUser[] Users =
        {
            new("johnsmith",     "john@arytix.com",     "John Smith",         IsCritic: true,  Bookings: 3, Favorites: 4, Reviews: 2),
            new("emmadavis",     "emma@arytix.com",     "Emma Davis",         IsCritic: true,  Bookings: 2, Favorites: 5, Reviews: 3),
            new("liamoneill",    "liam@arytix.com",     "Liam O'Neill",       IsCritic: true,  Bookings: 2, Favorites: 3, Reviews: 2),
            new("michaelchen",   "michael@arytix.com",  "Michael Chen",       IsCritic: false, Bookings: 3, Favorites: 2, Reviews: 1),
            new("sarahwilliams", "sarah@arytix.com",    "Sarah Williams",     IsCritic: false, Bookings: 1, Favorites: 4, Reviews: 1),
            new("davidbrown",    "david@arytix.com",    "David Brown",        IsCritic: false, Bookings: 2, Favorites: 1, Reviews: 0),
            new("oliviagarcia",  "olivia@arytix.com",   "Olivia Garcia",      IsCritic: false, Bookings: 2, Favorites: 3, Reviews: 1),
            new("jameswilson",   "james@arytix.com",    "James Wilson",       IsCritic: false, Bookings: 1, Favorites: 2, Reviews: 0),
            new("sophiamartin",  "sophia@arytix.com",   "Sophia Martinez",    IsCritic: false, Bookings: 3, Favorites: 6, Reviews: 2),
            new("noahtaylor",    "noah@arytix.com",     "Noah Taylor",        IsCritic: false, Bookings: 0, Favorites: 2, Reviews: 0),
        };

        private static readonly string[] ReviewSnippets =
        {
            "A devastating, beautifully staged piece — the kind of evening you carry home for days.",
            "Sharp, urgent, and unexpectedly funny in the cruellest moments. Casting was perfect.",
            "Bold direction, restrained design, exquisite performances. Highly recommended.",
            "The pacing dragged in the second act, but the final scene redeems everything.",
            "A masterclass in ensemble work — every supporting role lands.",
            "Visually striking, emotionally cold. I admired it more than I felt it.",
            "Worth the ticket for the lead performance alone. Riveting from first line to last.",
            "Funny, mean, and quietly heartbreaking. Reza translates exquisitely to this stage.",
            "Old text, fresh blood. I'd see it again tomorrow.",
            "Uneven, but when it works it really works. The ending floored me.",
        };

        private static readonly string[] MotivationSamples =
        {
            "I've been writing about theatre for a regional weekly for four years and want a wider audience for serious criticism.",
            "Theatre is the only place where ambition still feels like a moral act. I'd be honoured to contribute critically to AryTix.",
            "After completing a Masters in Performance Studies, I'm looking for a public-facing platform that takes the work seriously.",
        };

        public static async Task SeedAsync(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager)
        {
            // Only seed when fresh — bootstrap admin only.
            if (await db.Users.CountAsync() > 1) return;
            // Need productions and future performances to attach things to.
            var productions = await db.Productions.AsNoTracking().ToListAsync();
            if (productions.Count == 0) return;
            var futurePerfs = await db.Performances.AsNoTracking()
                .Where(p => p.IsActive && p.ShowDateTime > DateTime.UtcNow)
                .ToListAsync();
            if (futurePerfs.Count == 0) return;

            var rng = new Random(42);
            var perfsByProduction = futurePerfs.GroupBy(p => p.ProductionId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var createdUsers = new List<ApplicationUser>();
            foreach (var demo in Users)
            {
                var user = new ApplicationUser
                {
                    UserName = demo.UserName,
                    Email = demo.Email,
                    EmailConfirmed = true,
                    IsCritic = demo.IsCritic,
                    CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(30, 540))
                };
                var result = await userManager.CreateAsync(user, "Pass123!");
                if (!result.Succeeded) continue;
                await userManager.AddToRoleAsync(user, "User");
                createdUsers.Add(user);

                // Favourites
                foreach (var prod in PickRandom(productions, demo.Favorites, rng))
                {
                    db.UserFavorites.Add(new UserFavorite
                    {
                        UserId = user.Id,
                        ProductionId = prod.Id,
                        ProductionTitle = prod.Title,
                        PosterUrl = prod.PosterUrl
                    });
                }

                // Reviews
                foreach (var prod in PickRandom(productions, demo.Reviews, rng))
                {
                    db.UserReviews.Add(new UserReview
                    {
                        UserId = user.Id,
                        UserName = user.UserName!,
                        IsCritic = demo.IsCritic,
                        ProductionId = prod.Id,
                        ProductionTitle = prod.Title,
                        Rating = 6 + rng.Next(0, 5),
                        Content = ReviewSnippets[rng.Next(ReviewSnippets.Length)],
                        CreatedAt = DateTime.UtcNow.AddDays(-rng.Next(2, 90))
                    });
                }

                // Bookings against future performances — seats taken from the chart.
                for (var i = 0; i < demo.Bookings; i++)
                {
                    var perf = futurePerfs[rng.Next(futurePerfs.Count)];
                    var production = productions.FirstOrDefault(p => p.Id == perf.ProductionId);
                    if (production == null) continue;

                    var seatCount = rng.Next(1, 4);
                    var seats = AvailableSeats(db, perf.Id, seatCount, rng);
                    if (seats.Count == 0) continue;

                    var booking = new Booking
                    {
                        UserId = user.Id,
                        UserEmail = user.Email!,
                        UserName = user.UserName!,
                        ProductionTitle = production.Title,
                        PerformanceDateTime = perf.FormattedDateTime,
                        Stage = perf.Stage,
                        Seats = string.Join(", ", seats),
                        TotalPrice = perf.Price * seats.Count,
                        BookedAt = DateTime.UtcNow.AddDays(-rng.Next(0, 30))
                            .AddHours(-rng.Next(0, 24)),
                        ConfirmationCode = Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                        PerformanceId = perf.Id
                    };
                    db.Bookings.Add(booking);
                    await db.SaveChangesAsync();   // need booking.Id for reservations

                    foreach (var seat in seats)
                    {
                        db.SeatReservations.Add(new SeatReservation
                        {
                            PerformanceId = perf.Id,
                            BookingId = booking.Id,
                            SeatNumber = seat
                        });
                    }
                }

                await db.SaveChangesAsync();
            }

            // Existing-application examples: one pending, one approved (mirrors critic flag).
            var pendingApplicant = createdUsers.FirstOrDefault(u => u.UserName == "michaelchen");
            if (pendingApplicant != null && !await db.CriticApplications.AnyAsync(a => a.UserId == pendingApplicant.Id))
            {
                db.CriticApplications.Add(new CriticApplication
                {
                    UserId = pendingApplicant.Id,
                    FullName = "Michael Chen",
                    Bio = "Drama-school graduate; freelance contributor to a local arts blog for the past three seasons.",
                    YearsOfExperience = 3,
                    PastEmployers = "Curtain Up Magazine, StageNotes (freelance)",
                    ReviewLinksJson = JsonSerializer.Serialize(new[]
                    {
                        "https://example.com/reviews/king-lear",
                        "https://example.com/reviews/three-sisters"
                    }),
                    Motivation = MotivationSamples[0],
                    Status = ApplicationStatus.Pending,
                    AppliedAt = DateTime.UtcNow.AddDays(-4)
                });
            }

            foreach (var critic in createdUsers.Where(u => Users.First(d => d.UserName == u.UserName).IsCritic))
            {
                if (await db.CriticApplications.AnyAsync(a => a.UserId == critic.Id)) continue;
                db.CriticApplications.Add(new CriticApplication
                {
                    UserId = critic.Id,
                    FullName = Users.First(d => d.UserName == critic.UserName).FullName,
                    Bio = "Established theatre critic with bylines in regional and national press.",
                    YearsOfExperience = 6 + rng.Next(0, 9),
                    PastEmployers = "The Stage Review, Arts Quarterly",
                    ReviewLinksJson = JsonSerializer.Serialize(new[] { "https://example.com/portfolio" }),
                    Motivation = MotivationSamples[rng.Next(MotivationSamples.Length)],
                    Status = ApplicationStatus.Approved,
                    AppliedAt = critic.CreatedAt.AddDays(rng.Next(1, 10)),
                    ReviewedAt = critic.CreatedAt.AddDays(rng.Next(10, 25))
                });
            }

            await db.SaveChangesAsync();
        }

        private static IEnumerable<Production> PickRandom(List<Production> all, int count, Random rng)
            => all.OrderBy(_ => rng.Next()).Take(Math.Min(count, all.Count));

        private static List<string> AvailableSeats(ApplicationDbContext db, int perfId, int want, Random rng)
        {
            // Pool of seat labels matches BookingController.GenerateSeatingChart.
            var pool = new List<string>();
            var rows = "ABCDEFGH";
            foreach (var r in rows)
            {
                var maxSeat = r == 'H' ? 8 : 12; // approximates the chart's row sizes
                for (var n = 1; n <= maxSeat; n++) pool.Add($"{r}{n}");
            }
            var taken = db.SeatReservations
                .Where(sr => sr.PerformanceId == perfId)
                .Select(sr => sr.SeatNumber)
                .ToHashSet();
            var free = pool.Where(s => !taken.Contains(s)).OrderBy(_ => rng.Next()).Take(want).ToList();
            return free;
        }
    }
}
