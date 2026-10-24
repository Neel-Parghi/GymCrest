using System.Threading.Tasks;
using GymCrest.Contracts.SuperAdmin.Configuration;

namespace GymCrest.Application.Modules.SuperAdmin.Interfaces
{
    public interface ISaaSConfigurationService
    {
        Task<SaaSConfigurationDto> GetConfigurationAsync();
        Task UpdateConfigurationAsync(SaaSConfigurationDto configDto);
    }
}
