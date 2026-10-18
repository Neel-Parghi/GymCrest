using GymForge.Contracts.Gym.Management;
using GymForge.Domain.Entities;
using GymForge.Domain.Interface;
using GymForge.Shared.Helpers;
using Microsoft.Extensions.Logging;

namespace GymForge.Application.BackgroundJobs
{
    public class SaaSExpiryJob
    {
        private readonly IGymManagementRepository _gymRepository;
        private readonly ISaaSPaymentRepository _saasPaymentRepository;
        private readonly IEmailService _emailService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SaaSExpiryJob> _logger;

        public SaaSExpiryJob(
            IGymManagementRepository gymRepository,
            ISaaSPaymentRepository saasPaymentRepository,
            IEmailService emailService,
            IUnitOfWork unitOfWork,
            ILogger<SaaSExpiryJob> logger)
        {
            _gymRepository = gymRepository;
            _saasPaymentRepository = saasPaymentRepository;
            _emailService = emailService;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("SaaSExpiryJob started.");

            List<GymListResponseDto> gyms = await _gymRepository.GetAllGymsAsync();
            // Runs at 01:00 IST (still the previous day in UTC), so compare IST calendar dates.
            DateTime today = AppTimeZone.Today;

            foreach (var gym in gyms)
            {
                try
                {
                    SubscriptionRecord? subscription = await _saasPaymentRepository.GetLatestSubscriptionByGymIdAsync(gym.Id);
                    if (subscription == null) continue;

                    Gym? gymEntity = await _gymRepository.GetGymByIdAsync(gym.Id);
                    if (gymEntity == null || gymEntity.Owner == null || string.IsNullOrEmpty(gymEntity.Owner.Email)) continue;

                    DateTime endDate = AppTimeZone.ToLocalDate(subscription.EndDate);

                    if (endDate == today.AddDays(-1) && subscription.IsActive)
                    {
                        subscription.IsActive = false;

                        string ownerName = $"{gymEntity.Owner.FirstName} {gymEntity.Owner.LastName}";
                        string subject = $"Action Required: GymCrest Pro Plan Expired for {gymEntity.GymName}";
                        string body = $@"
                            <p>Hi {ownerName},</p>
                            <p>Your GymCrest subscription for <strong>{gymEntity.GymName}</strong> has expired on {endDate:MMM dd, yyyy}.</p>
                            <p>To restore full access for you and your staff, please log in to your dashboard and renew your subscription in the Billing section.</p>
                            <br/>
                            <p>Best regards,<br/>The GymCrest Team</p>
                        ";

                        await _emailService.SendEmailAsync(gymEntity.Owner.Email, ownerName, subject, body);
                        _logger.LogInformation($"Sent SaaS expiry email to {gymEntity.Owner.Email} for gym {gym.Id}.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed to process SaaSExpiryJob for gym {gym.Id}.");
                }
            }

            await _unitOfWork.SaveChangesAsync();
            _logger.LogInformation("SaaSExpiryJob completed.");
        }
    }
}
