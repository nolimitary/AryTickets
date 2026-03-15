using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AryTickets.Models
{
    public class CriticApplicationViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [Display(Name = "Full Name")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Bio is required")]
        [Display(Name = "About You")]
        public string Bio { get; set; }

        [Range(0, 50)]
        [Display(Name = "Years of Experience")]
        public int YearsOfExperience { get; set; }

        [Display(Name = "Past Employers / Publications")]
        public string PastEmployers { get; set; }

        [Display(Name = "Links to Past Reviews")]
        public List<string> ReviewLinks { get; set; } = new List<string>();

        [Required(ErrorMessage = "Please tell us why you want to be a critic")]
        [Display(Name = "Why do you want to become a critic?")]
        public string Motivation { get; set; }
    }
}