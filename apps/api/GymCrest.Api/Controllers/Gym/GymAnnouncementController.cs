using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.Announcements;
using GymCrest.Contracts.Members;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GymCrest.Api.Filters;
namespace GymCrest.Api.Controllers.Gym
{
    [ApiController]
    [Route("api/announcements")]
    [RequireModuleAccess("announcements")]
    public class GymAnnouncementController : BaseApiController
    {
        private readonly IGymAnnouncementService _service;

        public GymAnnouncementController(IGymAnnouncementService service)
        {
            _service = service;
        }

        [HttpPost]
        [BlockDemoAccount]
        [Authorize(Roles = "GymOwner,Staff")]
        public async Task<IActionResult> CreateAnnouncement([FromBody] GymAnnouncementRequest request)
        {
            if (GymId == null) 
                return BadRequest("GymId not found");
            
            GymAnnouncementResponse result = await _service.CreateAnnouncementAsync(GymId.Value, SecureBranchId, request);
            
            return Ok(result);
        }

        [HttpGet]
        [Authorize(Roles = "GymOwner,Staff")]
        public async Task<IActionResult> GetAllAnnouncements()
        {
            if (GymId == null) 
                return BadRequest("GymId not found");
           
            IEnumerable<GymAnnouncementResponse> result = await _service.GetAllAnnouncementsAsync(GymId.Value);
            
            return Ok(result);
        }

        [HttpPut("{id}")]
        [BlockDemoAccount]
        [Authorize(Roles = "GymOwner,Staff")]
        public async Task<IActionResult> UpdateAnnouncement(Guid id, [FromBody] GymAnnouncementRequest request)
        {
            if (GymId == null) 
                return BadRequest("GymId not found");
            
            GymAnnouncementResponse? result = await _service.UpdateAnnouncementAsync(id, GymId.Value, request);
            
            if (result == null)
                return BadRequest("Announcement not found");
            return Ok(result);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "GymOwner,Staff")]
        public async Task<IActionResult> DeleteAnnouncement(Guid id)
        {
            if (GymId == null) 
                return BadRequest("GymId not found");
            
            bool result = await _service.DeleteAnnouncementAsync(id, GymId.Value);
            
            if (!result) 
                return BadRequest("Announcement not found");
            return Ok();
        }

        [HttpGet("my-gym")]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> GetMyGymAnnouncements([FromServices] IGymMemberService memberService)
        {
            MyGymMembershipResponse membership = await memberService.GetMyGymMembershipAsync(UserId);
            
            if (membership == null)
            {
                return Ok(Array.Empty<GymAnnouncementResponse>());
            }

            IEnumerable<GymAnnouncementResponse> result = await _service.GetActiveAnnouncementsAsync(membership.GymId);
            
            return Ok(result);
        }
    }
}
