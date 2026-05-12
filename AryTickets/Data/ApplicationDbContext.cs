using AryTickets.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AryTickets.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Production> Productions { get; set; }
        public DbSet<Performance> Performances { get; set; }
        public DbSet<UserFavorite> UserFavorites { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<CriticApplication> CriticApplications { get; set; }
        public DbSet<UserReview> UserReviews { get; set; }
        public DbSet<SeatReservation> SeatReservations { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Prevent double-booking: one seat per performance
            modelBuilder.Entity<SeatReservation>()
                .HasIndex(sr => new { sr.PerformanceId, sr.SeatNumber })
                .IsUnique();
        }
    }
}
