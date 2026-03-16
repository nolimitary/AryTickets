using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace AryTickets.Services
{
    public class FileEmailSender : IEmailSender
    {
        private readonly string _filePath;

        public FileEmailSender(IConfiguration configuration)
        {
            _filePath = configuration["EmailSettings:FilePath"];
        }

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            return SendEmailWithAttachmentAsync(email, subject, htmlMessage, null, null);
        }

        public Task SendEmailWithAttachmentAsync(string email, string subject, string htmlMessage, byte[] attachment, string attachmentName)
        {
            var emailFileName = $"{System.Guid.NewGuid()}.eml";
            var fullPath = Path.Combine(_filePath, emailFileName);

            Directory.CreateDirectory(_filePath);

            var mailContent = $"To: {email}\nSubject: {subject}\nMIME-Version: 1.0\nContent-Type: text/html; charset=UTF-8\n\n{htmlMessage}";

            if (attachment != null && !string.IsNullOrEmpty(attachmentName))
            {
                var attachmentPath = Path.Combine(_filePath, attachmentName);
                File.WriteAllBytes(attachmentPath, attachment);
            }

            return File.WriteAllTextAsync(fullPath, mailContent);
        }
    }
}