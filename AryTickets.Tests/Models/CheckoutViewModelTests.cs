using AryTickets.Models;
using Xunit;

namespace AryTickets.Tests.Models
{
    public class CheckoutViewModelTests
    {
        [Fact]
        public void DefaultConstruction_StringPropertiesNonNull()
        {
            var model = new CheckoutViewModel();
            Assert.NotNull(model.ProductionTitle);
            Assert.NotNull(model.PerformanceDateTime);
            Assert.NotNull(model.Stage);
            Assert.NotNull(model.SelectedSeats);
        }

        [Fact]
        public void StripePaymentIntentId_NullByDefault()
        {
            var model = new CheckoutViewModel();
            Assert.Null(model.StripePaymentIntentId);
        }

        [Fact]
        public void CardFields_AreOptionalForStripePath()
        {
            var model = new CheckoutViewModel
            {
                ProductionTitle = "Хамлет",
                TotalPrice = 45m,
                StripePaymentIntentId = "pi_test_123"
            };
            // Simulated fields stay null — the Stripe path doesn't need them.
            Assert.Null(model.CardNumber);
            Assert.Null(model.Cvc);
            Assert.Null(model.ExpiryDate);
            Assert.Equal("pi_test_123", model.StripePaymentIntentId);
        }
    }
}
