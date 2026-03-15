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

        public DbSet<UserFavorite> UserFavorites { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<CriticApplication> CriticApplications { get; set; }
    }
}