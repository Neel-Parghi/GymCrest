using GymCrest.Application.Modules.SuperAdmin.Interfaces;
using GymCrest.Contracts.SuperAdmin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrest.Api.Controllers.SuperAdmin
{
    [ApiController]
    [Route("api/superadmin/demo-data")]
    [Authorize(Roles = "SuperAdmin")]
    public class DemoDataController : ControllerBase
    {
        private readonly IDemoDataService _demoDataService;

        public DemoDataController(IDemoDataService demoDataService)
        {
            _demoDataService = demoDataService;
        }

        [HttpPost("reset-member")]
        public async Task<IActionResult> ResetDemoMember()
        {
            DemoDataResetResultDto result = await _demoDataService.ResetDemoMemberAsync();
            return Ok(result);
        }
    }
}
