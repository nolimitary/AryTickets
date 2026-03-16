using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Threading.Tasks;

namespace AryTickets.Services
{
    public class SendGridEmailSender : IEmailSender
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;

        public SendGridEmailSender(IConfiguration configuration)
        {
            _apiKey = configuration["EmailSettings:SendGridApiKey"];
            _fromEmail = configuration["EmailSettings:FromEmail"];
            _fromName = configuration["EmailSettings:FromName"];
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            await SendEmailWithAttachmentAsync(email, subject, htmlMessage, null, null);
        }

        public async Task SendEmailWithAttachmentAsync(string email, string subject, string htmlMessage, byte[] attachment, string attachmentName)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new System.Exception("SendGrid API Key is not configured.");
            }

            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_fromEmail, _fromName);
            var to = new EmailAddress(email);
            var msg = MailHelper.CreateSingleEmail(from, to, subject, "", htmlMessage);

            if (attachment != null && !string.IsNullOrEmpty(attachmentName))
            {
                msg.AddAttachment(attachmentName, System.Convert.ToBase64String(attachment), "application/pdf");
            }

            await client.SendEmailAsync(msg);
        }
    }
}