using GymCrest.Application.Modules.SuperAdmin.Interfaces;
using GymCrest.Contracts.SuperAdmin.Configuration;

namespace GymCrest.Api.Middlewares
{
    /// <summary>
    /// While maintenance mode is active, answers API calls with 503 so the app can show its maintenance page.
    /// Super admins keep full access, and signing in, the health check and the public status stay open.
    /// </summary>
    public class MaintenanceMiddleware
    {
        public const string MaintenanceCode = "MAINTENANCE";

        private static readonly string[] OpenPrefixes = ["/api/health", "/api/public/", "/api/auth/"];

        private readonly RequestDelegate _next;

        public MaintenanceMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IPlatformStatusService statusService)
        {
            string path = context.Request.Path.Value ?? string.Empty;
            bool isApi = path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
            bool alwaysOpen = OpenPrefixes.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));

            if (!isApi || alwaysOpen || HttpMethods.IsOptions(context.Request.Method) || context.User.IsInRole("SuperAdmin"))
            {
                await _next(context);
                return;
            }

            PlatformStatusDto status = await statusService.GetStatusAsync();
            if (!status.MaintenanceActive)
            {
                await _next(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            if (status.MaintenanceEndTime.HasValue)
            {
                int seconds = (int)Math.Max((status.MaintenanceEndTime.Value - DateTime.UtcNow).TotalSeconds, 60);
                context.Response.Headers.RetryAfter = seconds.ToString();
            }
            await context.Response.WriteAsJsonAsync(new
            {
                Success = false,
                StatusCode = StatusCodes.Status503ServiceUnavailable,
                Code = MaintenanceCode,
                Message = "GymCrest is down for scheduled maintenance. Please try again shortly.",
                MaintenanceEndTime = status.MaintenanceEndTime
            });
        }
    }
}
