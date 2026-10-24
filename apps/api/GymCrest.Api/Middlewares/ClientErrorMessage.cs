namespace GymCrest.Api.Middlewares
{
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

            bool thrownInternally = ex.Source != null && InternalSources.Any(s => ex.Source.StartsWith(s, StringComparison.Ordinal));

            return userFacingType && !thrownInternally;
        }

        public static string For(Exception ex) => IsExpected(ex) ? ex.Message : Generic;
    }
}
