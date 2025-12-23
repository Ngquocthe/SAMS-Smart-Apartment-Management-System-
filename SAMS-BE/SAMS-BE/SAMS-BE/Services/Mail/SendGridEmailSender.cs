using Microsoft.Extensions.Options;
using SAMS_BE.Config.SendGrid;
using SendGrid.Helpers.Mail;
using SendGrid;
using SAMS_BE.Interfaces.IMail;

namespace SAMS_BE.Services.Mail
{
    public class SendGridEmailSender : IEmailSender
    {
        private readonly SendGridOptions _options;

        public SendGridEmailSender(IOptions<SendGridOptions> options)
        {
            _options = options.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlContent, string? plainTextContent = null)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new InvalidOperationException("SendGrid ApiKey is not configured.");

            var client = new SendGridClient(_options.ApiKey);

            var from = new EmailAddress(_options.FromEmail, _options.FromName);
            var to = new EmailAddress(toEmail);

            var msg = MailHelper.CreateSingleEmail(
                from,
                to,
                subject,
                plainTextContent ?? StripHtml(htmlContent), 
                htmlContent
            );

            var response = await client.SendEmailAsync(msg);

            if ((int)response.StatusCode >= 400)
            {
                var body = await response.Body.ReadAsStringAsync();
                throw new Exception($"SendGrid send failed: {(int)response.StatusCode} - {body}");
            }
        }

        // đơn giản, chấp nhận tạm
        private static string StripHtml(string html)
            => System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
    }
}
