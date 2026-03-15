using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace AryTickets.Services
{
    public class ResendEmailSender : IEmailSender
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly IHttpClientFactory _httpClientFactory;

        public ResendEmailSender(IConfiguration configuration, IHttpClientFactory httpClientFactory)
        {
            _apiKey = configuration["EmailSettings:ResendApiKey"] ?? "";
            _fromEmail = configuration["EmailSettings:FromEmail"] ?? "onboarding@resend.dev";
            _httpClientFactory = httpClientFactory;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new System.Exception("Resend API Key is not configured.");
            }

            var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");

            var payload = new
            {
                from = _fromEmail,
                to = new[] { email },
                subject = subject,
                html = htmlMessage
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync("https://api.resend.com/emails", content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new System.Exception($"Resend email failed: {error}");
            }
        }
    }
}