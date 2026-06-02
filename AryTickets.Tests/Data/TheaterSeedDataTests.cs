using AryTickets.Data;
using System;
using System.Linq;
using Xunit;

namespace AryTickets.Tests.Data
{
    public class TheaterSeedDataTests
    {
        [Fact]
        public void GetProductions_ReturnsAtLeastTwentyFive()
        {
            var productions = TheaterSeedData.GetProductions();
            Assert.True(productions.Count >= 25,
                $"Seed should contain at least 25 productions, got {productions.Count}.");
        }

        [Fact]
        public void GetProductions_AllTitlesUnique()
        {
            var productions = TheaterSeedData.GetProductions();
            var distinctTitles = productions.Select(p => p.Title).Distinct().Count();
            Assert.Equal(productions.Count, distinctTitles);
        }

        [Fact]
        public void GetProductions_AllHaveRequiredFields()
        {
            var productions = TheaterSeedData.GetProductions();

            Assert.All(productions, p =>
            {
                Assert.False(string.IsNullOrWhiteSpace(p.Title));
                Assert.False(string.IsNullOrWhiteSpace(p.Synopsis));
                Assert.False(string.IsNullOrWhiteSpace(p.Director));
                Assert.False(string.IsNullOrWhiteSpace(p.Cast));
                Assert.False(string.IsNullOrWhiteSpace(p.Genre));
                Assert.False(string.IsNullOrWhiteSpace(p.PosterUrl));
                Assert.False(string.IsNullOrWhiteSpace(p.BackdropUrl));
                Assert.True(p.DurationMinutes > 0);
                Assert.True(p.IsActive);
            });
        }

        [Fact]
        public void GetProductions_PosterUrlsAreValidEndpointsOrExternalImages()
        {
            var productions = TheaterSeedData.GetProductions();
            Assert.All(productions, p =>
            {
                // Either the in-process SVG endpoint (fallback) or an absolute
                // https URL to a real public-domain image.
                bool IsValid(string url) =>
                    url.StartsWith("/posters/", StringComparison.Ordinal) ||
                    url.StartsWith("https://", StringComparison.Ordinal);
                Assert.True(IsValid(p.PosterUrl), $"PosterUrl invalid: {p.PosterUrl}");
                Assert.True(IsValid(p.BackdropUrl), $"BackdropUrl invalid: {p.BackdropUrl}");

                // When the URL is the SVG fallback, the Title must be embedded.
                if (p.PosterUrl.StartsWith("/posters/", StringComparison.Ordinal))
                    Assert.Contains(Uri.EscapeDataString(p.Title), p.PosterUrl);
            });
        }

        [Fact]
        public void GetProductions_RatingsWithinExpectedRange()
        {
            var productions = TheaterSeedData.GetProductions();
            Assert.All(productions, p =>
                Assert.InRange(p.Rating, 0.0, 10.0));
        }

        [Fact]
        public void GetProductions_IsIdempotent_ReturnsSameTitles()
        {
            var first = TheaterSeedData.GetProductions().Select(p => p.Title).OrderBy(t => t).ToList();
            var second = TheaterSeedData.GetProductions().Select(p => p.Title).OrderBy(t => t).ToList();
            Assert.Equal(first, second);
        }
    }
}
