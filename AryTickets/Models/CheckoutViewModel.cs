using System.ComponentModel.DataAnnotations;

namespace AryTickets.Models
{
    public class CheckoutViewModel
    {
        public string ProductionTitle { get; set; }
        public string PerformanceDateTime { get; set; }
        public string Stage { get; set; }
        public string SelectedSeats { get; set; }
        public decimal TotalPrice { get; set; }
        public int? PerformanceId { get; set; }

        [Required]
        [Display(Name = "Име на картодържателя")]
        public string CardHolderName { get; set; }

        [Required]
        [Display(Name = "Номер на карта")]
        public string CardNumber { get; set; }

        [Required]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/?([0-9]{2})$", ErrorMessage = "Срокът трябва да е във формат MM/ГГ.")]
        [Display(Name = "Срок (MM/ГГ)")]
        public string ExpiryDate { get; set; }

        [Required]
        [StringLength(4, MinimumLength = 3)]
        [Display(Name = "CVC")]
        public string Cvc { get; set; }
    }
}
