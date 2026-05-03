using System.Net;
using System.Net.Mail;

namespace MembershipSystem.API.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public void SendEmail(string toEmail, string subject, string body)
        {
            var smtpServer = _config["EmailSettings:SmtpServer"];
            var port = int.TryParse(_config["EmailSettings:Port"], out var p) ? p : 587;
            var senderEmail = _config["EmailSettings:SenderEmail"];
            var password = _config["EmailSettings:Password"];

            var client = new SmtpClient(smtpServer, port)
            {
                Credentials = new NetworkCredential(senderEmail, password),
                EnableSsl = true
            };

            var mail = new MailMessage(senderEmail, toEmail, subject, body);
            mail.IsBodyHtml = true;

            client.Send(mail);
        }
    }
}