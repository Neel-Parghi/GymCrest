using GymForge.Application.Modules.SuperAdmin.Interfaces;
using GymForge.Contracts.SuperAdmin.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymForge.Api.Controllers
{
    /// <summary>Public platform status for the website and app: support contacts, grace period, maintenance.</summary>
    [ApiController]
    [Route("api/public/platform-status")]
    [AllowAnonymous]
    public class PlatformStatusController : ControllerBase
    {
        private readonly IPlatformStatusService _statusService;

        public PlatformStatusController(IPlatformStatusService statusService)
        {
            _statusService = statusService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            PlatformStatusDto status = await _statusService.GetStatusAsync();
            return Ok(status);
        }
    }
}
