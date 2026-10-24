using System.Security.Claims;
using GymCrest.Application.Modules.Gym.Interfaces;
using GymCrest.Shared.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GymCrest.Api.Filters
{
    /// <summary>
    /// Checks that the record named by the request belongs to the caller's gym or account
    /// (see <see cref="IResourceAccessService"/>). The id comes from a route value, or from a bound
    /// argument property written as "argument.Property". GET requests are reads; other methods are writes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizeResourceAttribute : TypeFilterAttribute
    {
        public AuthorizeResourceAttribute(OwnedResource resource, string idSource = "id") : base(typeof(AuthorizeResourceFilter))
        {
            Arguments = [resource, idSource];
        }
    }

    public class AuthorizeResourceFilter : IAsyncActionFilter
    {
        private readonly OwnedResource _resource;
        private readonly string _idSource;
        private readonly IResourceAccessService _accessService;

        public AuthorizeResourceFilter(OwnedResource resource, string idSource, IResourceAccessService accessService)
        {
            _resource = resource;
            _idSource = idSource;
            _accessService = accessService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            ClaimsPrincipal user = context.HttpContext.User;

            // A missing id is left to the action's own validation.
            if (!TryReadId(context, out Guid id))
            {
                await next();
                return;
            }

            bool allowed = Guid.TryParse(user.FindFirst("userId")?.Value, out Guid userId)
                && await _accessService.CanAccessAsync(
                    _resource,
                    id,
                    new ResourceCaller(
                        userId,
                        Guid.TryParse(user.FindFirst("gymId")?.Value, out Guid gymId) ? gymId : null,
                        IsGymOwner: user.IsInRole("GymOwner"),
                        IsGymStaff: user.IsInRole("Staff") || user.IsInRole("Trainer")),
                    write: !HttpMethods.IsGet(context.HttpContext.Request.Method));

            if (allowed)
            {
                await next();
                return;
            }

            context.Result = new ObjectResult(new { message = "You don't have access to this record." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        private bool TryReadId(ActionExecutingContext context, out Guid id)
        {
            id = Guid.Empty;
            string[] path = _idSource.Split('.', 2);

            if (path.Length == 1)
            {
                return Guid.TryParse(context.RouteData.Values[_idSource]?.ToString(), out id);
            }

            if (!context.ActionArguments.TryGetValue(path[0], out object? argument) || argument == null)
            {
                return false;
            }

            object? value = argument.GetType().GetProperty(path[1])?.GetValue(argument);
            if (value is Guid guid && guid != Guid.Empty)
            {
                id = guid;
                return true;
            }

            return false;
        }
    }
}
