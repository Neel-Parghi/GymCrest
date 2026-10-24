using GymCrest.Application.Modules.SuperAdmin.Interfaces;
using GymCrest.Contracts.SuperAdmin.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrest.Api.Controllers.SuperAdmin
{
    [ApiController]
    [Route("api/superadmin/dashboard")]
    [Authorize(Roles = "SuperAdmin")]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            SuperAdminDashboardDto stats = await _dashboardService.GetDashboardStatsAsync();
            return Ok(stats);
        }
    }
}
