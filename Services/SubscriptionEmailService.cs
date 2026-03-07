using System.Net;
using System.Net.Mail;

namespace IT15_DairyFlow.Services
{
    public class SubscriptionEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SubscriptionEmailService> _logger;

        public SubscriptionEmailService(IConfiguration configuration, ILogger<SubscriptionEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Sends a subscription confirmation email.
        /// Falls back to logging if SMTP is not configured.
        /// </summary>
        public async Task SendSubscriptionConfirmationAsync(string toEmail, string planName, bool isFreeTrial)
        {
            var subject = isFreeTrial
                ? "Welcome to DairyFlow — Free Trial Activated!"
                : $"Welcome to DairyFlow — {planName} Subscription Confirmed!";

            var body = BuildEmailHtml(toEmail, planName, isFreeTrial);

            var smtpHost = _configuration["Smtp:Host"];

            if (string.IsNullOrWhiteSpace(smtpHost))
            {
                _logger.LogWarning("SMTP not configured. Email to {Email} logged instead.", toEmail);
                _logger.LogInformation("Subject: {Subject}", subject);
                _logger.LogInformation("Body (first 200 chars): {Body}", body[..Math.Min(200, body.Length)]);
                return;
            }

            try
            {
                var port = int.Parse(_configuration["Smtp:Port"] ?? "587");
                var username = _configuration["Smtp:Username"] ?? "";
                var password = _configuration["Smtp:Password"] ?? "";
                var fromEmail = _configuration["Smtp:FromEmail"] ?? "noreply@dairyflow.com";
                var fromName = _configuration["Smtp:FromName"] ?? "DairyFlow ERP";

                using var smtpClient = new SmtpClient(smtpHost, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(fromEmail, fromName),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await smtpClient.SendMailAsync(mailMessage);

                _logger.LogInformation("Subscription confirmation email sent to {Email}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send subscription email to {Email}. The subscription was still created successfully.", toEmail);
            }
        }

        private static string BuildEmailHtml(string email, string planName, bool isFreeTrial)
        {
            var companyName = email.Split('@')[0];
            var planDescription = isFreeTrial
                ? "Your <strong>14-day Free Trial</strong> is now active. Explore all features of DairyFlow ERP at no cost."
                : $"Your <strong>{planName}</strong> subscription is now active. Thank you for choosing DairyFlow ERP!";

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
</head>
<body style='margin:0;padding:0;background-color:#f0f4f8;font-family:Inter,Arial,sans-serif;'>
    <div style='max-width:600px;margin:40px auto;background:#ffffff;border-radius:16px;overflow:hidden;box-shadow:0 4px 24px rgba(0,0,0,0.08);'>
        <!-- Header -->
        <div style='background:linear-gradient(135deg,#0d6efd,#0b5ed7);padding:40px 32px;text-align:center;'>
            <h1 style='color:#ffffff;margin:0;font-size:28px;font-weight:700;'>DairyFlow ERP</h1>
            <p style='color:rgba(255,255,255,0.85);margin:8px 0 0;font-size:14px;'>Integrated Dairy Manufacturing System</p>
        </div>

        <!-- Body -->
        <div style='padding:40px 32px;'>
            <div style='text-align:center;margin-bottom:24px;'>
                <div style='width:64px;height:64px;background:#e7f3ff;border-radius:50%;display:inline-flex;align-items:center;justify-content:center;'>
                    <span style='font-size:32px;'>&#10003;</span>
                </div>
            </div>

            <h2 style='color:#212529;text-align:center;font-size:22px;margin:0 0 8px;'>
                {(isFreeTrial ? "Trial Activated!" : "Subscription Confirmed!")}
            </h2>

            <p style='color:#6c757d;text-align:center;font-size:15px;margin:0 0 24px;'>
                {planDescription}
            </p>

            <div style='background:#f8fbff;border:1px solid #dbe7f3;border-radius:12px;padding:20px;margin:24px 0;'>
                <table style='width:100%;border-collapse:collapse;'>
                    <tr>
                        <td style='padding:8px 0;color:#6c757d;font-size:14px;'>Company</td>
                        <td style='padding:8px 0;color:#212529;font-size:14px;font-weight:600;text-align:right;'>{companyName}</td>
                    </tr>
                    <tr>
                        <td style='padding:8px 0;color:#6c757d;font-size:14px;'>Email</td>
                        <td style='padding:8px 0;color:#212529;font-size:14px;font-weight:600;text-align:right;'>{email}</td>
                    </tr>
                    <tr>
                        <td style='padding:8px 0;color:#6c757d;font-size:14px;'>Plan</td>
                        <td style='padding:8px 0;color:#0d6efd;font-size:14px;font-weight:600;text-align:right;'>{planName}</td>
                    </tr>
                </table>
            </div>

            <p style='color:#6c757d;font-size:14px;text-align:center;margin:24px 0 0;'>
                You can now log in to the DairyFlow ERP system using your registered credentials.
            </p>
        </div>

        <!-- Footer -->
        <div style='background:#f8f9fa;padding:20px 32px;text-align:center;border-top:1px solid #eef2f7;'>
            <p style='color:#adb5bd;font-size:12px;margin:0;'>&copy; 2026 DairyFlow ERP. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";
        }
    }
}
