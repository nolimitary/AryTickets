using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AryTickets.Models
{
    public class CheckoutViewModel
    {
        public string MovieTitle { get; set; }
        public string Showtime { get; set; }
        public string SelectedSeats { get; set; }
        public decimal TotalPrice { get; set; }

        [Required]
        [Display(Name = "Name on Card")]
        public string CardHolderName { get; set; }

        [Required]
        [CreditCard]
        [Display(Name = "Card Number")]
        public string CardNumber { get; set; }

        [Required]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/?([0-9]{2})$", ErrorMessage = "Expiry date must be in MM/YY format.")]
        [Display(Name = "Expiry Date (MM/YY)")]
        public string ExpiryDate { get; set; }

        [Required]
        [StringLength(4, MinimumLength = 3)]
        [Display(Name = "CVC")]
        public string Cvc { get; set; }
    }
}