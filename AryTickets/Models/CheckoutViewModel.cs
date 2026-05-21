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

        [Display(Name = "Име на картодържателя")]
        public string? CardHolderName { get; set; }

        // Card fields are only populated for the simulated (no-Stripe) fallback flow.
        [Display(Name = "Номер на карта")]
        public string? CardNumber { get; set; }

        [Display(Name = "Срок (MM/ГГ)")]
        public string? ExpiryDate { get; set; }

        [Display(Name = "CVC")]
        public string? Cvc { get; set; }

        // Populated by Stripe.js after a successful client-side PaymentIntent confirmation.
        public string? StripePaymentIntentId { get; set; }
    }
}
