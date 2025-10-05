using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace AryTickets.Models
{
    public class ProfileViewModel
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public DateTime JoinDate { get; set; }
        public List<Badge> Badges { get; set; }
    }
}