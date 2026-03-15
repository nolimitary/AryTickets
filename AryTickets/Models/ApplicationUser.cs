using Microsoft.AspNetCore.Identity;
using System;

namespace AryTickets.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? EmailVerificationCode { get; set; }
        public DateTime? VerificationCodeExpiry { get; set; }
        public bool IsCritic { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}