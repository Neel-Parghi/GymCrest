using GymCrest.Contracts.Gym.Billing;

namespace GymCrest.Application.Modules.Gym.Interfaces
{
    public interface IStaffPayrollService
    {
        Task<StaffPayrollOverviewDto> GetStaffPayrollOverviewAsync(Guid gymId, Guid? branchId, string monthKey);
        Task<bool> UpdateStaffPayrollRuleAsync(Guid gymId, UpdateStaffPayrollRuleRequest request);
        Task<bool> ReleaseStaffPayoutAsync(Guid gymId, Guid? branchId, ReleaseStaffPayoutRequest request);
    }
}
