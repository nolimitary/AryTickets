using AryTickets.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using Moq.Protected;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace AryTickets.Tests.Services
{
    public class EmailSenderTests
    {
        [Fact]
        public async Task ResendEmailSender_NoApiKey_ThrowsException()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "EmailSettings:ResendApiKey", "" }
                })
                .Build();
            var factory = new Mock<IHttpClientFactory>();

            var sender = new ResendEmailSender(config, factory.Object);

            await Assert.ThrowsAsync<Exception>(
                () => sender.SendEmailAsync("test@test.com", "Subject", "<p>Hello</p>"));
        }

        [Fact]
        public async Task ResendEmailSender_SuccessfulSend_DoesNotThrow()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "EmailSettings:ResendApiKey", "test-key" },
                    { "EmailSettings:FromEmail", "test@test.com" }
                })
                .Build();

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"id\":\"test\"}")
                });

            var httpClient = new HttpClient(mockHandler.Object);
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var sender = new ResendEmailSender(config, factory.Object);

            await sender.SendEmailAsync("recipient@test.com", "Test Subject", "<p>Test</p>");
            // No exception = success
        }

        [Fact]
        public async Task ResendEmailSender_ApiReturnsError_ThrowsWithMessage()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "EmailSettings:ResendApiKey", "test-key" },
                    { "EmailSettings:FromEmail", "test@test.com" }
                })
                .Build();

            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.Forbidden)
                {
                    Content = new StringContent("{\"message\":\"domain not verified\"}")
                });

            var httpClient = new HttpClient(mockHandler.Object);
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var sender = new ResendEmailSender(config, factory.Object);

            var ex = await Assert.ThrowsAsync<Exception>(
                () => sender.SendEmailAsync("recipient@test.com", "Test", "<p>Test</p>"));
            Assert.Contains("domain not verified", ex.Message);
        }

        [Fact]
        public async Task ResendEmailSender_WithAttachment_SendsCorrectPayload()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "EmailSettings:ResendApiKey", "test-key" },
                    { "EmailSettings:FromEmail", "test@test.com" }
                })
                .Build();

            HttpRequestMessage capturedRequest = null;
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"id\":\"test\"}")
                });

            var httpClient = new HttpClient(mockHandler.Object);
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var sender = new ResendEmailSender(config, factory.Object);
            var attachment = new byte[] { 0x25, 0x50, 0x44, 0x46 }; // PDF magic bytes

            await sender.SendEmailWithAttachmentAsync("to@test.com", "Subject", "<p>Hi</p>", attachment, "ticket.pdf");

            Assert.NotNull(capturedRequest);
            var body = await capturedRequest.Content.ReadAsStringAsync();
            Assert.Contains("ticket.pdf", body);
            Assert.Contains("attachments", body);
        }

        [Fact]
        public async Task ResendEmailSender_SendEmailAsync_CallsSendWithAttachmentNulls()
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "EmailSettings:ResendApiKey", "test-key" },
                    { "EmailSettings:FromEmail", "test@test.com" }
                })
                .Build();

            HttpRequestMessage capturedRequest = null;
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
                .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"id\":\"test\"}")
                });

            var httpClient = new HttpClient(mockHandler.Object);
            var factory = new Mock<IHttpClientFactory>();
            factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            var sender = new ResendEmailSender(config, factory.Object);
            await sender.SendEmailAsync("to@test.com", "Subject", "<p>Hi</p>");

            var body = await capturedRequest.Content.ReadAsStringAsync();
            Assert.DoesNotContain("attachments", body);
        }

        [Fact]
        public async Task FileEmailSender_WritesEmailToFile()
        {
            var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "arytix_test_" + Guid.NewGuid().ToString("N"));
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "EmailSettings:FilePath", tempDir }
                })
                .Build();

            var sender = new FileEmailSender(config);
            await sender.SendEmailAsync("test@test.com", "Test Subject", "<p>Hello</p>");

            var files = System.IO.Directory.GetFiles(tempDir, "*.eml");
            Assert.NotEmpty(files);

            var content = await System.IO.File.ReadAllTextAsync(files[0]);
            Assert.Contains("test@test.com", content);
            Assert.Contains("Test Subject", content);

            // Clean up
            System.IO.Directory.Delete(tempDir, true);
        }
    }
}
