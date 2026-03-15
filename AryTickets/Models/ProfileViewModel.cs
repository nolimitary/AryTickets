using System;
using System.Collections.Generic;

namespace AryTickets.Models
{
    public class ProfileViewModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public DateTime JoinDate { get; set; }
        public int TicketCount { get; set; }
        public int FavoritesCount { get; set; }
        public int ReviewsCount { get; set; }
        public decimal TotalSpent { get; set; }
        public List<Booking> RecentBookings { get; set; } = new List<Booking>();
        public bool IsCritic { get; set; }
        public bool HasPendingApplication { get; set; }
        public bool HasApplication { get; set; }
        public List<Badge> Badges { get; set; } = new List<Badge>();
    }
}