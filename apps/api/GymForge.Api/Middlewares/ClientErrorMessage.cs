namespace GymForge.Api.Middlewares
{
    /// <summary>
    /// Decides which exception messages may reach the client. The application throws plain
    /// <see cref="Exception"/> and a few standard types with user-facing text ("User not found.");
    /// anything else (database, driver, null references, HTTP clients) can expose internals and is
    /// replaced with a generic message.
    /// </summary>
    public static class ClientErrorMessage
    {
        public const string Generic = "Something went wrong. Please try again.";

        private static readonly string[] InternalSources = ["Microsoft.EntityFrameworkCore", "Npgsql", "System.Linq", "System.Private.CoreLib"];

        public static bool IsExpected(Exception ex)
        {
            Type type = ex.GetType();
            bool userFacingType = type == typeof(Exception)
                || type == typeof(InvalidOperationException)
                || type == typeof(KeyNotFoundException)
                || type == typeof(ArgumentException)
                || type == typeof(UnauthorizedAccessException);

            // The same types are also thrown by EF Core and the runtime ("Sequence contains no elements").
            bool thrownInternally = ex.Source != null && InternalSources.Any(s => ex.Source.StartsWith(s, StringComparison.Ordinal));

            return userFacingType && !thrownInternally;
        }

        /// <summary>The exception's message when it is meant for users, otherwise a generic one.</summary>
        public static string For(Exception ex) => IsExpected(ex) ? ex.Message : Generic;
    }
}
