using AryTickets.Services;
using QuestPDF.Infrastructure;
using Xunit;

namespace AryTickets.Tests.Services
{
    public class TicketPdfGeneratorTests
    {
        [Fact]
        public void Generate_ReturnsNonEmptyPdfBytes()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            var generator = new TicketPdfGenerator();

            // Use a non-existent QR URL to test the fallback (no QR image)
            var result = generator.Generate(
                "Test Movie",
                "Mar 20, 2026 - 7:00 PM",
                "A1, A2",
                25.00m,
                "CODE1234",
                "https://invalid-url-that-will-fail.test/qr.png"
            );

            Assert.NotNull(result);
            Assert.True(result.Length > 0);
            // PDF files start with %PDF
            Assert.Equal(0x25, result[0]); // %
            Assert.Equal(0x50, result[1]); // P
            Assert.Equal(0x44, result[2]); // D
            Assert.Equal(0x46, result[3]); // F
        }
    }
}
