using System.Security.Claims;
using GymForge.Application.Modules.Gym.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GymForge.Api.Filters
{
    /// <summary>
    /// Allows a member-scoped action only for that member's own account or staff of the member's gym
    /// (see <see cref="IMemberAccessService"/>). The member id is read from the given route value.
    /// </summary>
    public class AuthorizeMemberAccessAttribute : TypeFilterAttribute
    {
        public AuthorizeMemberAccessAttribute(string routeKey = "memberId") : base(typeof(AuthorizeMemberAccessFilter))
        {
            Arguments = [routeKey];
        }
    }

    public class AuthorizeMemberAccessFilter : IAsyncActionFilter
    {
        private readonly string _routeKey;
        private readonly IMemberAccessService _memberAccessService;

        public AuthorizeMemberAccessFilter(string routeKey, IMemberAccessService memberAccessService)
        {
            _routeKey = routeKey;
            _memberAccessService = memberAccessService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            ClaimsPrincipal user = context.HttpContext.User;

            bool allowed = Guid.TryParse(context.RouteData.Values[_routeKey]?.ToString(), out Guid memberId)
                && Guid.TryParse(user.FindFirst("userId")?.Value, out Guid userId)
                && await _memberAccessService.CanAccessMemberAsync(
                    memberId,
                    userId,
                    Guid.TryParse(user.FindFirst("gymId")?.Value, out Guid gymId) ? gymId : null,
                    isGymOwner: user.IsInRole("GymOwner"),
                    isGymStaff: user.IsInRole("Staff") || user.IsInRole("Trainer"));

            if (allowed)
            {
                await next();
                return;
            }

            context.Result = new ObjectResult(new { message = "You don't have access to this member." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }
}
