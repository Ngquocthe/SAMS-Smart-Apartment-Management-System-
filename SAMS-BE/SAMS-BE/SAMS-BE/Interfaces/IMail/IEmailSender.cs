namespace SAMS_BE.Interfaces.IMail
{
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlContent, string? plainTextContent = null);
    }
}
