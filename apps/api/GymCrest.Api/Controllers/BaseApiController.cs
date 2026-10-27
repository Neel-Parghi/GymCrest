using GymCrest.Api.Filters;
using GymCrest.Application.Modules.Users.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GymCrest.Api.Controllers
{
    [ApiController]
    [RequireActiveSubscription]
    public abstract class BaseApiController : ControllerBase, IAsyncActionFilter
    {
        private Guid? _standaloneUserGymId;

        protected Guid UserId
        {
            get
            {
                string? id = User.FindFirst("userId")?.Value;
                return string.IsNullOrEmpty(id) ? Guid.Empty : Guid.Parse(id);
            }
        }

        protected Guid? GymId
        {
            get
            {
                string? id = User.FindFirst("gymId")?.Value;
                if (!string.IsNullOrEmpty(id))
                    return Guid.Parse(id);

                return _standaloneUserGymId;
            }
        }

        protected Guid? SecureBranchId
        {
            get
            {
                if (User.IsInRole("GymOwner"))
                {
                    string? queryValue = Request.Query["branchId"].ToString();
                    return string.IsNullOrEmpty(queryValue) ? null : Guid.Parse(queryValue);
                }

                string? branchClaim = User.FindFirst("branchId")?.Value;
                return string.IsNullOrEmpty(branchClaim) ? null : Guid.Parse(branchClaim);
            }
        }

        protected bool IsStandaloneUser => User.IsInRole("User");

        [NonAction]
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (IsStandaloneUser && User.FindFirst("gymId") == null && UserId != Guid.Empty)
            {
                IUserService userService = HttpContext.RequestServices.GetRequiredService<IUserService>();
                _standaloneUserGymId = await userService.GetUserGymIdAsync(UserId);
            }

            await next();
        }
    }
}
