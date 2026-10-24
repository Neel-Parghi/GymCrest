using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Contracts.GymPlans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymCrest.Api.Controllers.User
{
    [Route("api/user-plans")]
    [Authorize(Roles = "User")]
    public class UserPlanController : BaseApiController
    {
        private readonly IGymPlanService _gymPlanService;

        public UserPlanController(IGymPlanService gymPlanService) 
        {
            _gymPlanService = gymPlanService;
        }

        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<GymPlanDto>>> GetAvailablePlans()
        {
            IEnumerable<GymPlanDto> activePlans = await _gymPlanService.GetAvailablePlansForMemberAsync(UserId);
            
            if (!activePlans.Any())
                return Ok(new List<GymPlanDto>());

            return Ok(activePlans);
        }
    }
}
