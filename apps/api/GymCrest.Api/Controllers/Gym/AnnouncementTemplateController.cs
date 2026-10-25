using GymCrest.Api.Filters;
using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.Announcements;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrest.Api.Controllers.Gym
{
    [ApiController]
    [Route("api/announcement-templates")]
    [Authorize(Roles = "GymOwner,Staff")]
    public class AnnouncementTemplateController : BaseApiController
    {
        private readonly ITemplateService _templateService;

        public AnnouncementTemplateController(ITemplateService templateService)
        {
            _templateService = templateService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTemplates()
        {
            if (GymId == null)
                return BadRequest("GymId not found");

            List<AnnouncementTemplateResponse> result = await _templateService.GetTemplatesAsync(GymId.Value, SecureBranchId);
            
            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTemplate(Guid id)
        {
            if (GymId == null)
                return BadRequest("GymId not found");

            AnnouncementTemplateResponse? result = await _templateService.GetTemplateByIdAsync(id, GymId.Value);
            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTemplate([FromBody] AnnouncementTemplateRequest request)
        {
            if (GymId == null)
                return BadRequest("GymId not found");

            AnnouncementTemplateResponse result = await _templateService.CreateTemplateAsync(request, GymId.Value, SecureBranchId, UserId);
            
            return Ok(result);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] AnnouncementTemplateRequest request)
        {
            if (GymId == null)
                return BadRequest("GymId not found");

            AnnouncementTemplateResponse result = await _templateService.UpdateTemplateAsync(id, request, GymId.Value, UserId);

            return Ok(result);
        }

        [HttpPost("{id}/test")]
        [BlockDemoAccount]
        public async Task<IActionResult> TestTemplate(Guid id)
        {
            if (GymId == null)
                return BadRequest("GymId not found");

            await _templateService.TestTemplateAsync(id, GymId.Value, UserId);
            return Ok(new { message = "Test notification and email sent successfully." });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTemplate(Guid id)
        {
            if (GymId == null)
                return BadRequest("GymId not found");

            await _templateService.DeleteTemplateAsync(id, GymId.Value);
            return Ok();
        }
    }
}
