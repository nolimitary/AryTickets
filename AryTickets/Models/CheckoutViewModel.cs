using System.ComponentModel.DataAnnotations;

namespace AryTickets.Models
{
    public class CheckoutViewModel
    {
        public string ProductionTitle { get; set; } = string.Empty;
        public string PerformanceDateTime { get; set; } = string.Empty;
        public string Stage { get; set; } = string.Empty;
        public string SelectedSeats { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public int? PerformanceId { get; set; }

        [Display(Name = "Cardholder name")]
        public string? CardHolderName { get; set; }

        // Card fields are only populated for the simulated (no-Stripe) fallback flow.
        [Display(Name = "Card number")]
        public string? CardNumber { get; set; }

        [Display(Name = "Expiry (MM/YY)")]
        public string? ExpiryDate { get; set; }

        [Display(Name = "CVC")]
        public string? Cvc { get; set; }

        // Populated by Stripe.js after a successful client-side PaymentIntent confirmation.
        public string? StripePaymentIntentId { get; set; }
    }
}
