using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.Gym.Billing;
using GymCrest.Contracts.Staff;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrest.Api.Controllers.Gym
{
    [Route("api/billing/staff")]
    [Authorize(Roles = "GymOwner,Staff,Trainer")]
    public class StaffPayrollController : BaseApiController
    {
        private readonly IStaffPayrollService _payrollService;
        private readonly IStaffService _staffService;

        public StaffPayrollController(IStaffPayrollService payrollService, IStaffService staffService)
        {
            _payrollService = payrollService;
            _staffService = staffService;
        }

        [HttpGet("overview")]
        public async Task<ActionResult> GetOverview([FromQuery] string monthKey)
        {
            if (GymId == null) 
                return Unauthorized();

            if (string.IsNullOrEmpty(monthKey))
            {
                monthKey = DateTime.UtcNow.ToString("yyyy-MM");
            }

            StaffPayrollOverviewDto overview = await _payrollService.GetStaffPayrollOverviewAsync(GymId.Value, SecureBranchId, monthKey);

            if (User.IsInRole("Trainer"))
            {
                StaffResponse? staff = await _staffService.GetStaffByIdAsync(UserId);
                if (staff != null)
                {
                    overview.Payouts = overview.Payouts.Where(p => p.StaffId == staff.Id).ToList();
                    overview.TotalBaseSalary = overview.Payouts.Sum(p => p.BaseSalary);
                    overview.TotalCommissions = overview.Payouts.Sum(p => p.Commissions);
                    overview.TotalPayout = overview.Payouts.Sum(p => p.TotalPayout);
                    overview.StaffCount = overview.Payouts.Count;
                }
                else
                {
                    overview.Payouts = new List<StaffPayoutDto>();
                    overview.TotalBaseSalary = 0;
                    overview.TotalCommissions = 0;
                    overview.TotalPayout = 0;
                    overview.StaffCount = 0;
                }
            }

            return Ok(overview);
        }

        [HttpPost("rules")]
        public async Task<ActionResult> UpdateRules([FromBody] UpdateStaffPayrollRuleRequest request)
        {
            if (GymId == null) 
                return Unauthorized();

            bool success = await _payrollService.UpdateStaffPayrollRuleAsync(GymId.Value, request);
            if (success)
            {
                return Ok(new { message = "Staff payroll rules updated successfully" });
            }

            return BadRequest(new { message = "Failed to update staff payroll rules" });
        }

        [HttpPost("payout/release")]
        public async Task<ActionResult> ReleasePayout([FromBody] ReleaseStaffPayoutRequest request)
        {
            if (GymId == null) 
                return Unauthorized();

            bool success = await _payrollService.ReleaseStaffPayoutAsync(GymId.Value, SecureBranchId, request);
            if (success)
            {
                return Ok(new { message = "Staff payout released successfully" });
            }

            return BadRequest(new { message = "Failed to release staff payout" });
        }
    }
}
