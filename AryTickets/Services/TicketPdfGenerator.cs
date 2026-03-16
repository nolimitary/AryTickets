using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace AryTickets.Services
{
    public class TicketPdfGenerator
    {
        public byte[] Generate(string movieTitle, string showtime, string seats, decimal totalPrice, string confirmationCode, string qrCodeUrl)
        {
            byte[] qrImageBytes = null;
            try
            {
                using var httpClient = new HttpClient();
                qrImageBytes = httpClient.GetByteArrayAsync(qrCodeUrl).GetAwaiter().GetResult();
            }
            catch { }

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(0);

                    page.Content().Column(col =>
                    {
                        // Header bar
                        col.Item().Background("#e11d48").Padding(20).Row(row =>
                        {
                            row.RelativeItem().Text(text =>
                            {
                                text.Span("Ary").FontSize(24).Bold().FontColor("#ffffff");
                                text.Span("Tix").FontSize(24).Light().FontColor("#ffffff");
                            });
                            row.ConstantItem(120).AlignRight().AlignMiddle()
                                .Text("TICKET").FontSize(12).Bold().FontColor("#ffffff").LetterSpacing(0.15f);
                        });

                        // Main content
                        col.Item().Background("#09090b").Padding(30).Column(inner =>
                        {
                            // Movie title
                            inner.Item().Text(movieTitle)
                                .FontSize(22).Bold().FontColor("#ffffff");

                            inner.Item().PaddingTop(20).LineHorizontal(1).LineColor("#1c1c1f");

                            // Details grid
                            inner.Item().PaddingTop(16).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("SHOWTIME").FontSize(9).FontColor("#71717a").LetterSpacing(0.1f);
                                    c.Item().PaddingTop(4).Text(showtime).FontSize(13).FontColor("#ffffff");
                                });
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("SEATS").FontSize(9).FontColor("#71717a").LetterSpacing(0.1f);
                                    c.Item().PaddingTop(4).Text(seats).FontSize(13).FontColor("#ffffff");
                                });
                            });

                            inner.Item().PaddingTop(16).Row(row =>
                            {
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("TOTAL PAID").FontSize(9).FontColor("#71717a").LetterSpacing(0.1f);
                                    c.Item().PaddingTop(4).Text($"${totalPrice:F2}").FontSize(18).Bold().FontColor("#e11d48");
                                });
                                row.RelativeItem().Column(c =>
                                {
                                    c.Item().Text("CONFIRMATION").FontSize(9).FontColor("#71717a").LetterSpacing(0.1f);
                                    c.Item().PaddingTop(4).Text(confirmationCode).FontSize(13).Bold().FontColor("#ffffff").LetterSpacing(0.15f);
                                });
                            });

                            inner.Item().PaddingTop(24).LineHorizontal(1).LineColor("#1c1c1f");

                            // QR Code
                            if (qrImageBytes != null)
                            {
                                inner.Item().PaddingTop(20).AlignCenter().Width(140).Image(qrImageBytes);
                            }

                            inner.Item().PaddingTop(12).AlignCenter()
                                .Text("Scan QR code or show this ticket at the entrance")
                                .FontSize(10).FontColor("#52525b");

                            inner.Item().PaddingTop(24).AlignCenter()
                                .Text($"© {DateTime.Now.Year} AryTix. All rights reserved.")
                                .FontSize(8).FontColor("#3f3f46");
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }
    }
}
