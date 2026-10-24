using GymCrest.Application.Modules.Payments.Interfaces;
using GymCrest.Application.Modules.SuperAdmin.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GymCrest.Api.Filters
{
    public class RequireActiveSubscriptionAttribute : TypeFilterAttribute
    {
        public RequireActiveSubscriptionAttribute() : base(typeof(RequireActiveSubscriptionFilter))
        {
        }
    }

    public class RequireActiveSubscriptionFilter : IAsyncActionFilter
    {
        private readonly ISaaSPaymentService _paymentService;
        private readonly IPlatformStatusService _platformStatus;

        public RequireActiveSubscriptionFilter(ISaaSPaymentService paymentService, IPlatformStatusService platformStatus)
        {
            _paymentService = paymentService;
            _platformStatus = platformStatus;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (context.ActionDescriptor is ControllerActionDescriptor actionDescriptor)
            {
                bool hasAllowAttribute = actionDescriptor.MethodInfo.GetCustomAttributes(typeof(AllowExpiredSubscriptionAttribute), true).Any() ||
                                         actionDescriptor.ControllerTypeInfo.GetCustomAttributes(typeof(AllowExpiredSubscriptionAttribute), true).Any();

                if (hasAllowAttribute)
                {
                    await next();
                    return;
                }
            }

            var user = context.HttpContext.User;
            if (user == null || !user.Identity?.IsAuthenticated == true)
            {
                await next();
                return;
            }

            // End Users are not affected by Gym SaaS Plan expiry
            if (user.IsInRole("User") && !user.IsInRole("GymOwner") && !user.IsInRole("Staff") && !user.IsInRole("Trainer") && !user.IsInRole("Manager"))
            {
                await next();
                return;
            }

            // Get GymId from user claims
            string? gymIdStr = user.FindFirst("gymId")?.Value;
            if (string.IsNullOrEmpty(gymIdStr) || !Guid.TryParse(gymIdStr, out Guid gymId))
            {
                await next();
                return;
            }

            // SuperAdmin is exempt
            if (user.IsInRole("SuperAdmin"))
            {
                await next();
                return;
            }

            var status = await _paymentService.GetSubscriptionStatusAsync(gymId);
            // The grace period keeps a lapsed gym working for a few days so the owner has time to renew
            int graceDays = (await _platformStatus.GetStatusAsync()).GracePeriodDays;
            if (status == null || !status.IsActive || status.EndDate.AddDays(graceDays) < DateTime.UtcNow)
            {
                context.Result = new ObjectResult(new { message = "Subscription Expired" })
                {
                    StatusCode = StatusCodes.Status402PaymentRequired
                };
                return;
            }

            await next();
        }
    }
}
