using GymForge.Application.Modules.SuperAdmin.Interfaces;
using GymForge.Contracts.SuperAdmin.Configuration;
using GymForge.Domain.Entities;
using GymForge.Domain.Interface;
using Microsoft.Extensions.Caching.Memory;

namespace GymForge.Application.Modules.SuperAdmin.Services
{
    public class PlatformStatusService : IPlatformStatusService
    {
        private const string CacheKey = "platform-status";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(30);

        private readonly ISaaSConfigurationRepository _configRepository;
        private readonly IMemoryCache _cache;

        public PlatformStatusService(ISaaSConfigurationRepository configRepository, IMemoryCache cache)
        {
            _configRepository = configRepository;
            _cache = cache;
        }

        public async Task<PlatformStatusDto> GetStatusAsync()
        {
            // Cache the settings, not the computed flag, so a maintenance window still opens and closes on time
            SaaSConfiguration? config = await _cache.GetOrCreateAsync(CacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                return await _configRepository.GetConfigurationAsync();
            });

            DateTime now = DateTime.UtcNow;
            return new PlatformStatusDto
            {
                SupportEmail = string.IsNullOrWhiteSpace(config?.SupportEmail) ? null : config.SupportEmail,
                SupportPhone = string.IsNullOrWhiteSpace(config?.SupportPhone) ? null : config.SupportPhone,
                GracePeriodDays = Math.Max(config?.GracePeriodDays ?? 0, 0),
                MaintenanceActive = config != null && config.IsMaintenanceMode
                    && (config.MaintenanceStartTime == null || now >= config.MaintenanceStartTime)
                    && (config.MaintenanceEndTime == null || now < config.MaintenanceEndTime),
                MaintenanceStartTime = config?.MaintenanceStartTime,
                MaintenanceEndTime = config?.MaintenanceEndTime
            };
        }

        public void Invalidate() => _cache.Remove(CacheKey);
    }
}
