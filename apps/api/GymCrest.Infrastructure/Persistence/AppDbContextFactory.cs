using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GymCrest.Infrastructure.Persistence
{
    /// <summary>
    /// Used exclusively by the EF Core CLI tools (migrations, database update).
    /// Ensures Npgsql is always selected - regardless of what appsettings.json contains.
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            string currentDir = Directory.GetCurrentDirectory();
            string basePath = currentDir.EndsWith("GymCrest.Api")
                ? currentDir
                : Path.Combine(currentDir, "../GymCrest.Api");

            if (!Directory.Exists(basePath))
                basePath = Path.Combine(currentDir, "GymCrest.Api");

            IConfigurationRoot config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)  // <-- picks up real DB
                .AddEnvironmentVariables()
                .Build();

            string? connectionString = config.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connectionString) || connectionString == "YOUR_CONNECTION_STRING_HERE")
                throw new InvalidOperationException(
                    "No valid DefaultConnection found. Ensure appsettings.Development.json contains a PostgreSQL connection string.");

            DbContextOptionsBuilder<AppDbContext> optionsBuilder = new();
            optionsBuilder.UseNpgsql(connectionString,
                sql => sql.EnableRetryOnFailure());

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}
