using GymCrest.Domain.Entities;

namespace GymCrest.Domain.Interface
{
    public interface ISaaSConfigurationRepository
    {
        Task<SaaSConfiguration> GetConfigurationAsync();
        Task UpdateConfigurationAsync(SaaSConfiguration configuration);
    }
}
