using GymCrest.Api.Filters;
using GymCrest.Application.Modules.Feedback.Interfaces;
using GymCrest.Contracts.Feedback;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;

namespace GymCrest.Api.Controllers
{
    [Route("api/feedback")]
    [Authorize]
    [AllowExpiredSubscription]
    public class FeedbackController : BaseApiController
    {
        private readonly IFeedbackService _feedbackService;

        public FeedbackController(IFeedbackService feedbackService)
        {
            _feedbackService = feedbackService;
        }

        [HttpPost]
        [EnableRateLimiting("FeedbackPolicy")]
        public async Task<IActionResult> Submit([FromBody] CreateFeedbackRequest request)
        {
            if (request == null)
                return BadRequest("Invalid request data.");

            try
            {
                string? email = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
                string? role = User.FindFirst(ClaimTypes.Role)?.Value;
                Guid? gymId = Guid.TryParse(User.FindFirst("gymId")?.Value, out Guid parsed) ? parsed : null;

                await _feedbackService.SubmitAsync(UserId, email, role, gymId, Request.Headers.UserAgent.ToString(), request);
                return Ok(new { message = "Thanks! Your feedback was sent." });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> GetAll()
        {
            List<FeedbackDto> items = await _feedbackService.GetAllAsync();
            return Ok(items);
        }

        [HttpPatch("{id}/resolved")]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> SetResolved(Guid id, [FromQuery] bool value = true)
        {
            bool updated = await _feedbackService.SetResolvedAsync(id, value);
            return updated ? Ok(new { message = "Updated." }) : NotFound("Feedback not found.");
        }
    }
}
