using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace AryTickets.Controllers
{
    // Generates SVG poster / backdrop images for theatre productions on the fly.
    // Replaces external placeholder services so every production has a thematic,
    // title-aware image without needing curated assets per show.
    [Route("posters")]
    public class PosterController : Controller
    {
        [HttpGet("poster")]
        [ResponseCache(Duration = 60 * 60 * 24 * 14, Location = ResponseCacheLocation.Any)]
        public IActionResult Poster(string title = "Untitled", string? playwright = null, string? genre = null)
        {
            var svg = BuildPosterSvg(title, playwright, genre);
            return Content(svg, "image/svg+xml");
        }

        [HttpGet("backdrop")]
        [ResponseCache(Duration = 60 * 60 * 24 * 14, Location = ResponseCacheLocation.Any)]
        public IActionResult Backdrop(string title = "Untitled", string? playwright = null)
        {
            var svg = BuildBackdropSvg(title, playwright);
            return Content(svg, "image/svg+xml");
        }

        private static string BuildPosterSvg(string title, string? playwright, string? genre)
        {
            var titleLines = WrapText(title, charsPerLine: 14);
            var hue = StableHue(title);

            var sb = new StringBuilder();
            sb.Append("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 600 900' preserveAspectRatio='xMidYMid slice'>");

            // Background — radial gradient in a stable, title-derived hue tinted toward the site's dark red.
            sb.Append("<defs>");
            sb.Append($"<radialGradient id='bg' cx='50%' cy='35%' r='75%'>");
            sb.Append($"<stop offset='0%' stop-color='hsl({hue},45%,18%)'/>");
            sb.Append("<stop offset='65%' stop-color='#2b0a0a'/>");
            sb.Append("<stop offset='100%' stop-color='#100303'/>");
            sb.Append("</radialGradient>");
            sb.Append("<linearGradient id='goldFade' x1='0%' y1='0%' x2='100%' y2='0%'>");
            sb.Append("<stop offset='0%' stop-color='#d4af37' stop-opacity='0'/>");
            sb.Append("<stop offset='50%' stop-color='#d4af37' stop-opacity='0.95'/>");
            sb.Append("<stop offset='100%' stop-color='#d4af37' stop-opacity='0'/>");
            sb.Append("</linearGradient>");
            sb.Append("</defs>");

            sb.Append("<rect width='600' height='900' fill='url(#bg)'/>");

            // Gold double-border, evoking a vintage playbill.
            sb.Append("<rect x='24' y='24' width='552' height='852' fill='none' stroke='#d4af37' stroke-opacity='0.75' stroke-width='2'/>");
            sb.Append("<rect x='36' y='36' width='528' height='828' fill='none' stroke='#d4af37' stroke-opacity='0.25' stroke-width='1'/>");

            // Top decorative bar with theatre masks.
            sb.Append("<g transform='translate(300,110)' opacity='0.85'>");
            // Comedy + tragedy masks as simple stylised circles with eyes/mouth.
            sb.Append("<path d='M -42,0 a 24,30 0 1 0 48,0 a 24,30 0 1 0 -48,0' fill='none' stroke='#d4af37' stroke-width='2'/>");
            sb.Append("<circle cx='-30' cy='-6' r='2.5' fill='#d4af37'/><circle cx='-6' cy='-6' r='2.5' fill='#d4af37'/>");
            sb.Append("<path d='M -28,12 Q -18,4 -8,12' fill='none' stroke='#d4af37' stroke-width='1.5'/>");
            sb.Append("<path d='M 6,0 a 24,30 0 1 0 48,0 a 24,30 0 1 0 -48,0' fill='none' stroke='#d4af37' stroke-width='2'/>");
            sb.Append("<circle cx='18' cy='-6' r='2.5' fill='#d4af37'/><circle cx='42' cy='-6' r='2.5' fill='#d4af37'/>");
            sb.Append("<path d='M 20,16 Q 30,22 40,16' fill='none' stroke='#d4af37' stroke-width='1.5'/>");
            sb.Append("</g>");

            sb.Append("<line x1='180' y1='180' x2='420' y2='180' stroke='url(#goldFade)' stroke-width='1'/>");
            sb.Append("<text x='300' y='208' text-anchor='middle' fill='#d4af37' font-family=\"Inter, sans-serif\" font-size='12' letter-spacing='6'>ARYTIX · STAGE</text>");

            // Title — wrapped, large serif.
            var startY = titleLines.Count switch { 1 => 470, 2 => 420, 3 => 380, _ => 350 };
            var fontSize = titleLines.Count switch { 1 => 64, 2 => 58, 3 => 48, _ => 40 };
            for (var i = 0; i < titleLines.Count; i++)
            {
                sb.Append($"<text x='300' y='{startY + i * (fontSize + 8)}' text-anchor='middle' fill='#f5e6d3' font-family=\"Playfair Display, Georgia, serif\" font-weight='600' font-size='{fontSize}'>");
                sb.Append(Escape(titleLines[i]));
                sb.Append("</text>");
            }

            // Playwright in italic serif beneath title.
            if (!string.IsNullOrWhiteSpace(playwright))
            {
                sb.Append($"<line x1='240' y1='620' x2='360' y2='620' stroke='#d4af37' stroke-opacity='0.55' stroke-width='1'/>");
                sb.Append($"<text x='300' y='660' text-anchor='middle' fill='#c9a961' font-family=\"Cormorant Garamond, Georgia, serif\" font-style='italic' font-size='26'>by {Escape(playwright)}</text>");
            }

            // Genre badge near the bottom.
            if (!string.IsNullOrWhiteSpace(genre))
            {
                sb.Append("<rect x='220' y='760' width='160' height='40' fill='none' stroke='#d4af37' stroke-opacity='0.6' stroke-width='1'/>");
                sb.Append($"<text x='300' y='787' text-anchor='middle' fill='#d4af37' font-family=\"Inter, sans-serif\" font-size='13' letter-spacing='5'>{Escape(genre.ToUpperInvariant())}</text>");
            }

            // Footer ornament.
            sb.Append("<text x='300' y='840' text-anchor='middle' fill='#d4af37' opacity='0.55' font-family=\"Playfair Display, Georgia, serif\" font-size='22'>❦</text>");

            sb.Append("</svg>");
            return sb.ToString();
        }

        private static string BuildBackdropSvg(string title, string? playwright)
        {
            var hue = StableHue(title);
            var sb = new StringBuilder();
            sb.Append("<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 2400 1000' preserveAspectRatio='xMidYMid slice'>");
            sb.Append("<defs>");
            sb.Append($"<radialGradient id='bg' cx='30%' cy='40%' r='80%'>");
            sb.Append($"<stop offset='0%' stop-color='hsl({hue},42%,22%)'/>");
            sb.Append("<stop offset='60%' stop-color='#2b0a0a'/>");
            sb.Append("<stop offset='100%' stop-color='#0a0202'/>");
            sb.Append("</radialGradient>");
            sb.Append("<linearGradient id='vignette' x1='0' y1='0' x2='0' y2='1'>");
            sb.Append("<stop offset='0%' stop-color='#000' stop-opacity='0'/>");
            sb.Append("<stop offset='100%' stop-color='#000' stop-opacity='0.6'/>");
            sb.Append("</linearGradient>");
            sb.Append("</defs>");
            sb.Append("<rect width='2400' height='1000' fill='url(#bg)'/>");
            sb.Append("<rect width='2400' height='1000' fill='url(#vignette)'/>");

            // Faint stage-curtain swags as decorative arcs.
            for (var i = 0; i < 5; i++)
            {
                var cx = 240 + i * 480;
                sb.Append($"<path d='M {cx - 200},0 Q {cx},220 {cx + 200},0' fill='none' stroke='#d4af37' stroke-opacity='0.08' stroke-width='2'/>");
            }

            // Soft glow behind the title.
            sb.Append("<circle cx='1200' cy='540' r='420' fill='#d4af37' opacity='0.05'/>");

            sb.Append($"<text x='1200' y='540' text-anchor='middle' fill='#f5e6d3' font-family=\"Playfair Display, Georgia, serif\" font-weight='600' font-size='180' opacity='0.92'>{Escape(title)}</text>");

            if (!string.IsNullOrWhiteSpace(playwright))
            {
                sb.Append($"<text x='1200' y='640' text-anchor='middle' fill='#c9a961' font-family=\"Cormorant Garamond, Georgia, serif\" font-style='italic' font-size='42' opacity='0.85'>by {Escape(playwright)}</text>");
            }

            sb.Append("</svg>");
            return sb.ToString();
        }

        private static System.Collections.Generic.List<string> WrapText(string text, int charsPerLine)
        {
            var lines = new System.Collections.Generic.List<string>();
            if (string.IsNullOrWhiteSpace(text)) { lines.Add(""); return lines; }

            var words = text.Trim().Split(' ', System.StringSplitOptions.RemoveEmptyEntries);
            var current = new StringBuilder();
            foreach (var word in words)
            {
                if (current.Length == 0)
                {
                    current.Append(word);
                }
                else if (current.Length + 1 + word.Length <= charsPerLine)
                {
                    current.Append(' ').Append(word);
                }
                else
                {
                    lines.Add(current.ToString());
                    current.Clear().Append(word);
                }
            }
            if (current.Length > 0) lines.Add(current.ToString());
            return lines;
        }

        private static int StableHue(string seed)
        {
            // Deterministic hash → 0–359 hue. Keeps each title visually distinct.
            unchecked
            {
                var hash = 23;
                foreach (var ch in seed) hash = hash * 31 + ch;
                return System.Math.Abs(hash) % 360;
            }
        }

        private static string Escape(string s) => System.Net.WebUtility.HtmlEncode(s);
    }
}
