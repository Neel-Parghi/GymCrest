using GymForge.Domain.Interface;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace GymForge.Infrastructure.Services
{
    /// <summary>
    /// Sends transactional email through Brevo templates. Wording, design and subject lines are edited in Brevo
    /// (Transactional → Email → Templates); this class only picks the template and fills its {{ params.* }} values.
    /// Template ids come from configuration: Brevo:Templates:{name} (Render: Brevo__Templates__{name}).
    /// Each template receives the params set below plus appUrl and year.
    /// </summary>
    public class BrevoEmailService : IEmailService
    {
        private enum EmailTemplate
        {
            Invitation,
            VerificationCode,
            PasswordReset,
            SaleReceipt,
            MembershipLinked,
            PlanExpired,
            GymAnnouncement
        }

        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;

        public BrevoEmailService(IConfiguration config, HttpClient httpClient)
        {
            _config = config;
            _httpClient = httpClient;
        }

        public Task SendInvitationEmailAsync(string email, string name, string token)
        {
            string inviteLink = $"{AppUrl()}/accept-invitation?token={Uri.EscapeDataString(token)}";
            return SendTemplateAsync(EmailTemplate.Invitation, email, name, new()
            {
                ["name"] = name,
                ["inviteLink"] = inviteLink
            });
        }

        public Task SendOtpEmailAsync(string email, string name, string otpCode) =>
            SendTemplateAsync(EmailTemplate.VerificationCode, email, name, new()
            {
                ["name"] = name,
                ["otpCode"] = otpCode
            });

        public Task SendPasswordResetLinkEmailAsync(string email, string name, string resetLink) =>
            SendTemplateAsync(EmailTemplate.PasswordReset, email, name, new()
            {
                ["name"] = name,
                ["resetLink"] = resetLink
            });

        public Task SendReceiptEmailAsync(string email, string name, string transactionId, string amount, string productName, string date) =>
            SendTemplateAsync(EmailTemplate.SaleReceipt, email, name, new()
            {
                ["name"] = name,
                ["transactionId"] = transactionId,
                ["amount"] = amount,
                ["productName"] = productName,
                ["date"] = date
            });

        public Task SendMembershipLinkedEmailAsync(string email, string name, string firstName) =>
            SendTemplateAsync(EmailTemplate.MembershipLinked, email, name, new()
            {
                ["firstName"] = firstName
            });

        public Task SendPlanExpiredEmailAsync(string email, string ownerName, string gymName, string endDate) =>
            SendTemplateAsync(EmailTemplate.PlanExpired, email, ownerName, new()
            {
                ["ownerName"] = ownerName,
                ["gymName"] = gymName,
                ["endDate"] = endDate
            });

        public Task SendGymAnnouncementEmailAsync(string email, string name, string gymName, string subject, string message) =>
            SendTemplateAsync(EmailTemplate.GymAnnouncement, email, name, new()
            {
                ["name"] = name,
                ["gymName"] = gymName,
                ["subject"] = subject,
                ["message"] = message
            });

        private string AppUrl() =>
            (_config["GymCrest:BaseUrl"] ?? throw new InvalidOperationException("GymCrest BaseUrl is missing.")).TrimEnd('/');

        private async Task SendTemplateAsync(EmailTemplate template, string email, string name, Dictionary<string, object> parameters)
        {
            string apiKey = _config["Brevo:RestApiKey"] ?? throw new InvalidOperationException("Brevo REST API Key (RestApiKey) is missing.");
            string fromEmail = _config["Brevo:FromEmail"] ?? throw new InvalidOperationException("Brevo FromEmail is missing.");
            string fromName = _config["Brevo:FromName"] ?? throw new InvalidOperationException("Brevo FromName is missing.");

            if (!long.TryParse(_config[$"Brevo:Templates:{template}"], out long templateId) || templateId <= 0)
                throw new InvalidOperationException(
                    $"Brevo template id for '{template}' is not configured. Set Brevo:Templates:{template} (Render: Brevo__Templates__{template}).");

            // Available in every template
            parameters["appUrl"] = AppUrl();
            parameters["year"] = DateTime.UtcNow.Year;

            var payload = new
            {
                sender = new { name = fromName, email = fromEmail },
                to = new[] { new { name, email } },
                templateId,
                @params = parameters
            };

            using HttpRequestMessage request = new(HttpMethod.Post, "https://api.brevo.com/v3/smtp/email");
            request.Headers.Add("api-key", apiKey);
            request.Content = JsonContent.Create(payload);

            HttpResponseMessage response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                string errorDetail = await response.Content.ReadAsStringAsync();
                throw new Exception($"Brevo API error sending '{template}' (template {templateId}): {response.StatusCode} - {errorDetail}");
            }
        }
    }
}
