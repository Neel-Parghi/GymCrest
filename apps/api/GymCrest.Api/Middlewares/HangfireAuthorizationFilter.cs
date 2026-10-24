using System.Security.Cryptography;
using System.Text;
using Hangfire.Dashboard;

namespace GymCrest.Api.Middlewares
{
    /// <summary>
    /// Guards the Hangfire dashboard, which can trigger and delete background jobs. Local requests in
    /// Development are allowed; everywhere else it needs HTTP Basic credentials from
    /// Hangfire:DashboardUser / Hangfire:DashboardPassword, and stays closed when those are not set.
    /// </summary>
    public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
    {
        private readonly string? _user;
        private readonly string? _password;
        private readonly bool _isDevelopment;

        public HangfireAuthorizationFilter(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _user = configuration["Hangfire:DashboardUser"];
            _password = configuration["Hangfire:DashboardPassword"];
            _isDevelopment = environment.IsDevelopment();
        }

        public bool Authorize(DashboardContext context)
        {
            HttpContext httpContext = context.GetHttpContext();

            if (_isDevelopment && IsLocal(httpContext))
            {
                return true;
            }

            if (string.IsNullOrEmpty(_user) || string.IsNullOrEmpty(_password))
            {
                return false;
            }

            if (HasValidBasicCredentials(httpContext))
            {
                return true;
            }

            // Ask the browser for credentials.
            httpContext.Response.Headers.WWWAuthenticate = "Basic realm=\"Hangfire\"";
            return false;
        }

        private static bool IsLocal(HttpContext httpContext)
        {
            System.Net.IPAddress? remote = httpContext.Connection.RemoteIpAddress;
            return remote != null && System.Net.IPAddress.IsLoopback(remote);
        }

        private bool HasValidBasicCredentials(HttpContext httpContext)
        {
            string header = httpContext.Request.Headers.Authorization.ToString();
            if (!header.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string decoded;
            try
            {
                decoded = Encoding.UTF8.GetString(Convert.FromBase64String(header["Basic ".Length..].Trim()));
            }
            catch (FormatException)
            {
                return false;
            }

            int separator = decoded.IndexOf(':');
            if (separator < 0)
            {
                return false;
            }

            return FixedTimeEquals(decoded[..separator], _user!) && FixedTimeEquals(decoded[(separator + 1)..], _password!);
        }

        private static bool FixedTimeEquals(string a, string b) =>
            CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));
    }
}
