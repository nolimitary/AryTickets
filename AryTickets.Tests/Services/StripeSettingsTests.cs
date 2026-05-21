using AryTickets.Services;
using Xunit;

namespace AryTickets.Tests.Services
{
    public class StripeSettingsTests
    {
        [Fact]
        public void IsConfigured_FalseWhenBothKeysEmpty()
        {
            var settings = new StripeSettings();
            Assert.False(settings.IsConfigured);
        }

        [Fact]
        public void IsConfigured_FalseWhenOnlySecretSet()
        {
            var settings = new StripeSettings { SecretKey = "sk_test_x" };
            Assert.False(settings.IsConfigured);
        }

        [Fact]
        public void IsConfigured_FalseWhenOnlyPublishableSet()
        {
            var settings = new StripeSettings { PublishableKey = "pk_test_x" };
            Assert.False(settings.IsConfigured);
        }

        [Fact]
        public void IsConfigured_TrueWhenBothSet()
        {
            var settings = new StripeSettings
            {
                SecretKey = "sk_test_x",
                PublishableKey = "pk_test_x"
            };
            Assert.True(settings.IsConfigured);
        }

        [Fact]
        public void IsConfigured_FalseWhenKeysAreWhitespace()
        {
            var settings = new StripeSettings
            {
                SecretKey = "   ",
                PublishableKey = "   "
            };
            Assert.False(settings.IsConfigured);
        }
    }
}
