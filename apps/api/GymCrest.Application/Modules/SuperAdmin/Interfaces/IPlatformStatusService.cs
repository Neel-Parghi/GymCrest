using GymCrest.Contracts.SuperAdmin.Configuration;

namespace GymCrest.Application.Modules.SuperAdmin.Interfaces
{
    public interface IPlatformStatusService
    {
        /// <summary>Support contacts, grace period and maintenance state; cached briefly because it is read on every request.</summary>
        Task<PlatformStatusDto> GetStatusAsync();

        /// <summary>Drops the cached copy so a settings change applies straight away.</summary>
        void Invalidate();
    }
}
