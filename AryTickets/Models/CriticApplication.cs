using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace AryTickets.Models
{
    public enum ApplicationStatus
    {
        Pending = 0,
        Approved = 1,
        Denied = 2
    }

    public class CriticApplication
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; }
        public ApplicationUser User { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string Bio { get; set; }

        public int YearsOfExperience { get; set; }

        public string PastEmployers { get; set; }

        public string ReviewLinksJson { get; set; } = "[]";

        [NotMapped]
        public List<string> ReviewLinks
        {
            get => string.IsNullOrEmpty(ReviewLinksJson) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(ReviewLinksJson);
            set => ReviewLinksJson = JsonSerializer.Serialize(value ?? new List<string>());
        }

        [Required]
        public string Motivation { get; set; }

        public ApplicationStatus Status { get; set; } = ApplicationStatus.Pending;
        public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReviewedAt { get; set; }
    }
}