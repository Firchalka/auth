using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PhotosApp.Services
{
    public class SimpleEmailSender : IEmailSender
    {
        private readonly bool _enableSsl;
        private readonly IWebHostEnvironment _env;
        private readonly string _host;
        private readonly ILogger<SimpleEmailSender> _logger;
        private readonly string _password;
        private readonly int _port;
        private readonly string _userName;

        public SimpleEmailSender(ILogger<SimpleEmailSender> logger,
            IWebHostEnvironment hostingEnvironment,
            string host, int port, bool enableSsl,
            string userName, string password)
        {
            _logger = logger;
            _env = hostingEnvironment;
            _host = host;
            _port = port;
            _enableSsl = enableSsl;
            _userName = userName;
            _password = password;
        }

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            if (_env.IsDevelopment())
            {
                var message = new StringBuilder();
                message.AppendLine();
                message.AppendLine(">>> -------------------- <<<");
                message.AppendLine($"From: {_userName}");
                message.AppendLine($"To: {email}");
                message.AppendLine($"Subject: {subject}");
                message.AppendLine();
                message.AppendLine(htmlMessage);
                message.AppendLine(">>> -------------------- <<<");
                message.AppendLine();
                _logger.LogInformation(message.ToString());
            }

            if (!string.IsNullOrEmpty(_userName) && !string.IsNullOrEmpty(_password))
            {
                var client = new SmtpClient(_host, _port)
                {
                    Credentials = new NetworkCredential(_userName, _password),
                    EnableSsl = _enableSsl
                };
                await client.SendMailAsync(
                    new MailMessage(_userName, email, subject, htmlMessage)
                    {
                        IsBodyHtml = true
                    }
                );
            }
        }
    }
}