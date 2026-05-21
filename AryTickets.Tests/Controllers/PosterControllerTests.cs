using AryTickets.Controllers;
using Microsoft.AspNetCore.Mvc;
using System.Xml.Linq;
using Xunit;

namespace AryTickets.Tests.Controllers
{
    public class PosterControllerTests
    {
        private static string GetSvg(IActionResult result)
        {
            var content = Assert.IsType<ContentResult>(result);
            Assert.Equal("image/svg+xml", content.ContentType);
            Assert.NotNull(content.Content);
            return content.Content!;
        }

        [Fact]
        public void Poster_ReturnsParseableSvg()
        {
            var controller = new PosterController();
            var svg = GetSvg(controller.Poster("Хамлет", "Уилям Шекспир", "Трагедия"));

            // If the SVG has malformed XML (unclosed tags, bad chars, etc.) XDocument throws here.
            var doc = XDocument.Parse(svg);
            Assert.Equal("svg", doc.Root!.Name.LocalName);
        }

        [Fact]
        public void Poster_TitleAppearsInOutput()
        {
            var controller = new PosterController();
            var svg = GetSvg(controller.Poster("Хамлет", "Уилям Шекспир", "Трагедия"));
            Assert.Contains("Хамлет", svg);
        }

        [Fact]
        public void Poster_WithoutPlaywrightOrGenre_StillValid()
        {
            var controller = new PosterController();
            var svg = GetSvg(controller.Poster("X"));
            var doc = XDocument.Parse(svg);
            Assert.Equal("svg", doc.Root!.Name.LocalName);
        }

        [Fact]
        public void Poster_HtmlSpecialCharsInTitle_DoNotBreakSvg()
        {
            var controller = new PosterController();
            var svg = GetSvg(controller.Poster("Master & Margarita", "Bulgakov & co", "Драма/Сатира"));
            var doc = XDocument.Parse(svg);
            Assert.Equal("svg", doc.Root!.Name.LocalName);
            // Ampersand must be entity-escaped — otherwise XDocument.Parse would have thrown above.
            Assert.Contains("&amp;", svg);
            Assert.DoesNotContain("Master & Margarita", svg);
        }

        [Fact]
        public void Backdrop_ReturnsParseableSvg()
        {
            var controller = new PosterController();
            var svg = GetSvg(controller.Backdrop("Хамлет", "Уилям Шекспир"));
            var doc = XDocument.Parse(svg);
            Assert.Equal("svg", doc.Root!.Name.LocalName);
        }

        [Fact]
        public void Backdrop_LongTitle_StillValid()
        {
            var controller = new PosterController();
            var svg = GetSvg(controller.Backdrop(
                "Много дълго заглавие на пиеса което може да наруши форматирането",
                "Анонимен Автор"));
            var doc = XDocument.Parse(svg);
            Assert.Equal("svg", doc.Root!.Name.LocalName);
        }
    }
}
