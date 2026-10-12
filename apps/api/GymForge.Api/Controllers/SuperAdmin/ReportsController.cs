using GymForge.Api.Middlewares;
using GymForge.Application.Modules.SuperAdmin.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymForge.Api.Controllers.SuperAdmin
{
    [ApiController]
    [Route("api/superadmin/reports")]
    [Authorize(Roles = "SuperAdmin")]
    [SkipResponseWrapper]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        [HttpGet("export")]
        public async Task<IActionResult> ExportData([FromQuery] string type)
        {
            if (string.IsNullOrEmpty(type))
                return BadRequest("Report type is required.");

            try
            {
                (byte[] content, string fileName) = await _reportService.GenerateReportAsync(type);
                return File(content, "text/csv", fileName);
            }
            catch (System.ArgumentException ex)
            {
                return BadRequest(ClientErrorMessage.For(ex));
            }
            catch (Exception)
            {
                return StatusCode(500, "An error occurred while generating the report.");
            }
        }
    }
}
