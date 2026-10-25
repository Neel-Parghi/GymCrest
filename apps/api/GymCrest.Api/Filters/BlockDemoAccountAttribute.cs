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

    /// <summary>Demo account emails and the message shown when the demo can't do something.</summary>
    public static class DemoAccounts
    {
        public const string BlockedMessage =
            "This isn't available in the demo. Create a free account to try it with your own gym.";

        public static bool IsDemo(HttpContext http, IConfiguration configuration)
        {
            string? email = http.User.FindFirst(ClaimTypes.Email)?.Value ?? http.User.FindFirst("email")?.Value;
            if (email == null) return false;
            string[] demoEmails = configuration.GetSection("Demo:Emails").Get<string[]>() ?? [];
            return demoEmails.Contains(email, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Registered globally: the shared demo gym (owner/staff) can't delete anything, so one visitor can't empty
    /// it for everyone else. Other risky actions (emails to typed-in addresses, uploads, branding) use
    /// [BlockDemoAccount] on the endpoint.
    /// </summary>
    public class BlockDemoDeletesFilter : IAsyncActionFilter
    {
        private readonly IConfiguration _configuration;

        public BlockDemoDeletesFilter(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            bool demoGymSide = !context.HttpContext.User.IsInRole("User") && DemoAccounts.IsDemo(context.HttpContext, _configuration);
            if (HttpMethods.IsDelete(context.HttpContext.Request.Method) && demoGymSide)
            {
                context.Result = new ObjectResult(new { message = DemoAccounts.BlockedMessage })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            await next();
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
                context.Result = new ObjectResult(new { message = DemoAccounts.BlockedMessage })
                {
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }

            await next();
        }
    }
}
