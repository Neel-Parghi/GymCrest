namespace GymForge.Domain.Interface
{
    /// <summary>
    /// Transactional email. Each method maps to a template managed in the email provider (wording, design and
    /// subject live there); callers only supply the values that fill it.
    /// </summary>
    public interface IEmailService
    {
        Task SendInvitationEmailAsync(string email, string name, string token);
        Task SendOtpEmailAsync(string email, string name, string otpCode);
        Task SendPasswordResetLinkEmailAsync(string email, string name, string resetLink);
        Task SendReceiptEmailAsync(string email, string name, string transactionId, string amount, string productName, string date);
        Task SendMembershipLinkedEmailAsync(string email, string name, string firstName);
        Task SendPlanExpiredEmailAsync(string email, string ownerName, string gymName, string endDate);

        /// <summary>Gym-written announcement (subject and message come from the gym's own templates).</summary>
        Task SendGymAnnouncementEmailAsync(string email, string name, string gymName, string subject, string message);
    }
}
