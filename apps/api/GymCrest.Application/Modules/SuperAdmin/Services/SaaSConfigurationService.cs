using System.Threading.Tasks;
using GymCrest.Application.Modules.SuperAdmin.Interfaces;
using GymCrest.Contracts.SuperAdmin.Configuration;
using GymCrest.Domain.Entities;
using GymCrest.Domain.Interface;

namespace GymCrest.Application.Modules.SuperAdmin.Services
{
    public class SaaSConfigurationService : ISaaSConfigurationService
    {
        private readonly ISaaSConfigurationRepository _configRepository;
        private readonly IPlatformStatusService _platformStatus;

        public SaaSConfigurationService(ISaaSConfigurationRepository configRepository, IPlatformStatusService platformStatus)
        {
            _configRepository = configRepository;
            _platformStatus = platformStatus;
        }

        public async Task<SaaSConfigurationDto> GetConfigurationAsync()
        {
            SaaSConfiguration config = await _configRepository.GetConfigurationAsync();
            
            return new SaaSConfigurationDto
            {
                PlatformName = config.PlatformName,
                BillingEmail = config.BillingEmail,
                TaxPercentage = config.TaxPercentage,
                GracePeriodDays = config.GracePeriodDays,
                Currency = config.Currency,
                BillingAddress = config.BillingAddress,
                GstNo = config.GstNo,
                SupportPhone = config.SupportPhone,
                SupportEmail = config.SupportEmail,
                IsMaintenanceMode = config.IsMaintenanceMode,
                IsFoodSearchEnabled = config.IsFoodSearchEnabled,
                TermsUrl = config.TermsUrl,
                PrivacyUrl = config.PrivacyUrl,
                MaintenanceStartTime = config.MaintenanceStartTime,
                MaintenanceEndTime = config.MaintenanceEndTime,
                YearlyRevenueTarget = config.YearlyRevenueTarget,
                SubscriptionTarget = config.SubscriptionTarget,
                UptimeThreshold = config.UptimeThreshold
            };
        }

        public async Task UpdateConfigurationAsync(SaaSConfigurationDto configDto)
        {
            SaaSConfiguration config = await _configRepository.GetConfigurationAsync();

            config.PlatformName = configDto.PlatformName;
            config.BillingEmail = configDto.BillingEmail;
            config.TaxPercentage = configDto.TaxPercentage;
            config.GracePeriodDays = configDto.GracePeriodDays;
            config.Currency = configDto.Currency;
            config.BillingAddress = configDto.BillingAddress;
            config.GstNo = configDto.GstNo;
            config.SupportPhone = configDto.SupportPhone;
            config.SupportEmail = configDto.SupportEmail;
            config.IsMaintenanceMode = configDto.IsMaintenanceMode;
            config.IsFoodSearchEnabled = configDto.IsFoodSearchEnabled;
            config.TermsUrl = configDto.TermsUrl;
            config.PrivacyUrl = configDto.PrivacyUrl;
            config.MaintenanceStartTime = configDto.MaintenanceStartTime;
            config.MaintenanceEndTime = configDto.MaintenanceEndTime;
            config.YearlyRevenueTarget = configDto.YearlyRevenueTarget;
            config.SubscriptionTarget = configDto.SubscriptionTarget;
            config.UptimeThreshold = configDto.UptimeThreshold;

            await _configRepository.UpdateConfigurationAsync(config);
            _platformStatus.Invalidate();
        }
    }
}
