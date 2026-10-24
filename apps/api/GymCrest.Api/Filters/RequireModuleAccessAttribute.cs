using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;
using GymCrest.Application.Modules.Gym.Interfaces;

namespace GymCrest.Api.Filters
{
    public class RequireModuleAccessAttribute : TypeFilterAttribute
    {
        public RequireModuleAccessAttribute(string moduleName) : base(typeof(RequireModuleAccessFilter))
        {
            Arguments = new object[] { moduleName };
        }
    }

    public class RequireModuleAccessFilter : IAsyncActionFilter
    {
        private readonly string _moduleName;
        private readonly IGymManagementService _gymService;

        public RequireModuleAccessFilter(string moduleName, IGymManagementService gymService)
        {
            _moduleName = moduleName;
            _gymService = gymService;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var user = context.HttpContext.User;

            if (user == null || !user.Identity?.IsAuthenticated == true)
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // SuperAdmin, GymOwner, Trainer, and Standalone Users automatically allowed
            if (user.IsInRole("SuperAdmin") || user.IsInRole("GymOwner") || user.IsInRole("Trainer") || user.IsInRole("User"))
            {
                await next();
                return;
            }

            if (user.IsInRole("Staff"))
            {
                string? gymIdString = user.FindFirst("gymId")?.Value;
                if (!Guid.TryParse(gymIdString, out Guid gymId))
                {
                    context.Result = new ForbidResult();
                    return;
                }

                var settings = await _gymService.GetGymSettingsAsync(gymId);
                if (settings == null)
                {
                    context.Result = new ForbidResult();
                    return;
                }

                var rights = new Dictionary<string, bool>();
                if (!string.IsNullOrEmpty(settings.RoleRightsMatrixJson))
                {
                    try
                    {
                        rights = JsonSerializer.Deserialize<Dictionary<string, bool>>(settings.RoleRightsMatrixJson) ?? new Dictionary<string, bool>();
                    }
                    catch
                    {
                        // Ignore parse errors, fall back to defaults
                    }
                }

                string matrixKey = $"Staff_{_moduleName}";

                bool hasAccess = true;
                bool isDefaultTrueModule = _moduleName == "dashboard" || _moduleName == "members" || _moduleName == "attendance" || _moduleName == "inventory";

                if (rights.TryGetValue(matrixKey, out bool value))
                {
                    hasAccess = value;
                }
                else
                {
                    hasAccess = isDefaultTrueModule;
                }

                if (!hasAccess)
                {
                    context.Result = new ForbidResult();
                    return;
                }
            }

            await next();
        }
    }
}
