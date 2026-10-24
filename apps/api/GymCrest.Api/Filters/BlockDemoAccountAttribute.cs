using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GymCrest.Api.Filters
{
    /// <summary>
    /// Refuses the action for the public demo accounts (Demo:Emails in configuration), so their shared
    /// credentials cannot be changed or the accounts removed by calling the API directly.
    /// </summary>
    public class BlockDemoAccountAttribute : TypeFilterAttribute
    {
        public BlockDemoAccountAttribute() : base(typeof(BlockDemoAccountFilter))
        {
        }
    }

    public class BlockDemoAccountFilter : IAsyncActionFilter
    {
        private readonly HashSet<string> _demoEmails;

        public BlockDemoAccountFilter(IConfiguration configuration)
        {
            _demoEmails = new HashSet<string>(
                configuration.GetSection("Demo:Emails").Get<string[]>() ?? [],
                StringComparer.OrdinalIgnoreCase);
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            string? email = context.HttpContext.User.FindFirst(ClaimTypes.Email)?.Value
                ?? context.HttpContext.User.FindFirst("email")?.Value;

            if (email != null && _demoEmails.Contains(email))
            {
                context.Result = new ObjectResult(new { message = "This action is disabled for the demo account." })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            await next();
        }
    }
}
