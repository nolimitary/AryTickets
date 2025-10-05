using Microsoft.AspNetCore.Identity;
using System;

namespace AryTickets.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string? EmailVerificationCode { get; set; }
        public DateTime? VerificationCodeExpiry { get; set; }
    }
}