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
        private readonly ISaaSConfigurationRepository _configRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SaaSExpiryJob> _logger;

        public SaaSExpiryJob(
            IGymManagementRepository gymRepository,
            ISaaSPaymentRepository saasPaymentRepository,
            IEmailService emailService,
            ISaaSConfigurationRepository configRepository,
            IUnitOfWork unitOfWork,
            ILogger<SaaSExpiryJob> logger)
        {
            _gymRepository = gymRepository;
            _saasPaymentRepository = saasPaymentRepository;
            _emailService = emailService;
            _configRepository = configRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            _logger.LogInformation("SaaSExpiryJob started.");

            List<GymListResponseDto> gyms = await _gymRepository.GetAllGymsAsync();
            // Runs at 01:00 IST (still the previous day in UTC), so compare IST calendar dates.
            DateTime today = AppTimeZone.Today;
            // Plans stay open through the grace period, so the gym keeps access until it ends
            int graceDays = Math.Max((await _configRepository.GetConfigurationAsync()).GracePeriodDays, 0);

            foreach (var gym in gyms)
            {
                try
                {
                    SubscriptionRecord? subscription = await _saasPaymentRepository.GetLatestSubscriptionByGymIdAsync(gym.Id);
                    if (subscription == null || !subscription.IsActive) continue;

                    DateTime endDate = AppTimeZone.ToLocalDate(subscription.EndDate);

                    // The plan's last day has passed. Catches up on plans that ended on a night the job didn't run,
                    // and closes the plan even when the owner can't be emailed.
                    if (endDate.AddDays(graceDays) >= today) continue;

                    subscription.IsActive = false;

                    Gym? gymEntity = await _gymRepository.GetGymByIdAsync(gym.Id);
                    if (gymEntity?.Owner == null || string.IsNullOrEmpty(gymEntity.Owner.Email)) continue;

                    string ownerName = $"{gymEntity.Owner.FirstName} {gymEntity.Owner.LastName}";
                    await _emailService.SendPlanExpiredEmailAsync(gymEntity.Owner.Email, ownerName, gymEntity.GymName, endDate.ToString("MMM dd, yyyy"));
                    _logger.LogInformation($"Sent SaaS expiry email to {gymEntity.Owner.Email} for gym {gym.Id}.");
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
