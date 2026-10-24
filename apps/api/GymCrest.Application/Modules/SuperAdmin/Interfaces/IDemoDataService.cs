using GymCrest.Contracts.SuperAdmin;

namespace GymCrest.Application.Modules.SuperAdmin.Interfaces
{
    public interface IDemoDataService
    {
        /// <summary>
        /// Wipes the demo member's activity and rebuilds it: fresh workout and diet plans, two weeks of
        /// logged workouts and meals ending yesterday, and four health check-ins.
        /// </summary>
        Task<DemoDataResetResultDto> ResetDemoMemberAsync();
    }
}
