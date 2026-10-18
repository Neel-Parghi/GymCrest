using GymForge.Domain.Interface;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace GymForge.Infrastructure.Services
{
    public class BrevoEmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public BrevoEmailService(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        public async Task SendInvitationEmailAsync(string email, string name, string token)
        {
            try
            {
                string apiKey = _config["Brevo:RestApiKey"] ?? throw new Exception("Brevo REST API Key (RestApiKey) is missing.");
                string fromEmail = _config["Brevo:FromEmail"] ?? throw new Exception("Brevo FromEmail is missing.");
                string fromName = _config["Brevo:FromName"] ?? throw new Exception("Brevo FromName is missing.");
                // GymCrest:BaseUrl is still honoured so hosts configured before the GymCrest rename keep working
                string baseUrl = _config["GymCrest:BaseUrl"]
                    ?? throw new Exception("GymCrest BaseUrl is missing.");

                string inviteLink = $"{baseUrl}/accept-invitation?token={Uri.EscapeDataString(token)}";

                var payload = new
                {
                    sender = new { name = fromName, email = fromEmail },
                    to = new[] { new { name = name, email = email } },
                    subject = "Invitation to Join GymCrest",
                    htmlContent = $"<strong>Hello {name},</strong><br><br>Please click <a href='{inviteLink}'>here</a> to set your password and join GymCrest."
                };

                using HttpRequestMessage request = new(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
                request.Headers.Add("api-key", apiKey);
                request.Content = JsonContent.Create(payload);

                HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    string errorDetail = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Brevo API error: {response.StatusCode} - {errorDetail}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while sending the invitation email via Brevo REST API.", ex);
            }
        }

        public async Task SendEmailAsync(string email, string name, string subject, string htmlContent)
        {
            try
            {
                string apiKey = _config["Brevo:RestApiKey"] ?? throw new Exception("Brevo REST API Key is missing.");
                string fromEmail = _config["Brevo:FromEmail"] ?? throw new Exception("Brevo FromEmail is missing.");
                string fromName = _config["Brevo:FromName"] ?? throw new Exception("Brevo FromName is missing.");

                var payload = new
                {
                    sender = new { name = fromName, email = fromEmail },
                    to = new[] { new { name = name, email = email } },
                    subject = subject,
                    htmlContent = htmlContent
                };

                using HttpRequestMessage request = new(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
                request.Headers.Add("api-key", apiKey);
                request.Content = JsonContent.Create(payload);

                HttpResponseMessage response = await _httpClient.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    string errorDetail = await response.Content.ReadAsStringAsync();
                    throw new Exception($"Brevo API error: {response.StatusCode} - {errorDetail}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("An error occurred while sending the email via Brevo.", ex);
            }
        }

        public async Task SendReceiptEmailAsync(string email, string name, string transactionId, string amount, string productName, string date)
        {
            string htmlContent = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #eee; padding: 20px; border-radius: 10px;'>
                    <h2 style='color: #2563eb; text-align: center;'>GymCrest Payment Receipt</h2>
                    <p>Hello <strong>{name}</strong>,</p>
                    <p>Thank you for your purchase. Here are your transaction details:</p>
                    <div style='background: #f9fafb; padding: 15px; border-radius: 8px; margin: 20px 0;'>
                        <table style='width: 100%; border-collapse: collapse;'>
                            <tr><td style='padding: 8px 0; color: #6b7280;'>Transaction ID:</td><td style='text-align: right; font-weight: bold;'>#{transactionId}</td></tr>
                            <tr><td style='padding: 8px 0; color: #6b7280;'>Product:</td><td style='text-align: right; font-weight: bold;'>{productName}</td></tr>
                            <tr><td style='padding: 8px 0; color: #6b7280;'>Amount Paid:</td><td style='text-align: right; font-weight: bold; color: #10b981;'>₹{amount}</td></tr>
                            <tr><td style='padding: 8px 0; color: #6b7280;'>Date:</td><td style='text-align: right;'>{date}</td></tr>
                        </table>
                    </div>
                    <p style='text-align: center; color: #9ca3af; font-size: 0.875rem;'>This is an automated receipt. No signature required.</p>
                </div>";

            await SendEmailAsync(email, name, $"Receipt for your purchase: {productName}", htmlContent);
        }

        public async Task SendOtpEmailAsync(string email, string name, string otpCode)
        {
            string htmlContent = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px;'>
                    <h2 style='color: #2c3e50; text-align: center;'>Welcome to GymCrest!</h2>
                    <p style='color: #555;'>Hello {name},</p>
                    <p style='color: #555;'>Thank you for registering. Please use the verification code below to complete your registration process.</p>
                    <div style='background-color: #f8f9fa; padding: 15px; border-radius: 4px; text-align: center; margin: 20px 0;'>
                        <h1 style='color: #007bff; margin: 0; letter-spacing: 5px; font-size: 32px;'>{otpCode}</h1>
                    </div>
                    <p style='color: #555;'>This code will expire in 10 minutes.</p>
                    <p style='color: #555;'>If you did not request this, please ignore this email.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                    <p style='color: #888; font-size: 12px; text-align: center;'>&copy; {DateTime.UtcNow.Year} GymCrest. All rights reserved.</p>
                </div>";

            await SendEmailAsync(email, name, "Your GymCrest Verification Code", htmlContent);
        }
        public async Task SendPasswordResetLinkEmailAsync(string email, string name, string resetLink)
        {
            string htmlContent = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 5px;'>
                    <h2 style='color: #2c3e50; text-align: center;'>Reset Your Password</h2>
                    <p style='color: #555;'>Hello {name},</p>
                    <p style='color: #555;'>We received a request to reset your password for your GymCrest account. Click the button below to securely set a new password.</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <a href='{resetLink}' style='background-color: #005bbc; color: white; padding: 12px 24px; text-decoration: none; border-radius: 6px; font-weight: bold; font-size: 16px;'>Reset Password</a>
                    </div>
                    <p style='color: #555; font-size: 14px;'>Or copy and paste this link into your browser:</p>
                    <p style='color: #005bbc; font-size: 13px; word-break: break-all;'>{resetLink}</p>
                    <p style='color: #555; margin-top: 20px;'>This link will expire in 10 minutes.</p>
                    <p style='color: #555;'>If you did not request a password reset, please ignore this email. Your account remains secure.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                    <p style='color: #888; font-size: 12px; text-align: center;'>&copy; {DateTime.UtcNow.Year} GymCrest. All rights reserved.</p>
                </div>";

            await SendEmailAsync(email, name, "GymCrest Password Reset Link", htmlContent);
        }
    }
}
