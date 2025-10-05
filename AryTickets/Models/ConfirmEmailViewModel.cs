using System.ComponentModel.DataAnnotations;

namespace AryTickets.Models
{
    public class ConfirmEmailViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Code { get; set; }
    }
}