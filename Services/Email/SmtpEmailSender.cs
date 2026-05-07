using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace EventsApp.Services.Email
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (!_configuration.GetValue("Email:Enabled", false))
            {
                _logger.LogInformation("Email sending is disabled. Skipping message to {Email} with subject {Subject}.", email, subject);
                return;
            }

            var host = _configuration["Email:Smtp:Host"];
            var username = _configuration["Email:Smtp:Username"];
            var fromEmail = _configuration["Email:From:Email"] ?? username;
            if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(fromEmail))
            {
                _logger.LogWarning("Email is enabled but SMTP host or from email is missing.");
                return;
            }

            var port = _configuration.GetValue("Email:Smtp:Port", 587);
            var enableSsl = _configuration.GetValue("Email:Smtp:EnableSsl", true);
            var password = _configuration["Email:Smtp:Password"];
            var fromName = _configuration["Email:From:Name"] ?? "Evento";

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
            };

            if (!string.IsNullOrWhiteSpace(username))
            {
                client.Credentials = new NetworkCredential(username, password);
            }

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true,
            };
            message.To.Add(email);

            await client.SendMailAsync(message);
        }
    }
}
