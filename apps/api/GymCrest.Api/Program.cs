using GymCrest.Api;
using GymCrest.Api.Middlewares;
using System.Threading.RateLimiting;
using GymCrest.Application;
using GymCrest.Infrastructure;
using Microsoft.EntityFrameworkCore;
using GymCrest.Infrastructure.Persistence;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.HttpOverrides;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => options.Filters.Add<GymCrest.Api.Filters.BlockDemoDeletesFilter>())
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new DateTimeJsonConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableDateTimeJsonConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

string[] allowedOrigins = new[]
    {
        builder.Configuration["GymCrest:BaseUrl"],
        "https://GymCrest.vercel.app",
        "http://localhost:4200"
    }
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Select(origin => origin!.TrimEnd('/'))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray();

string? previewOriginSuffix = builder.Configuration["GymCrest:PreviewOriginSuffix"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy =>
        {
            policy.AllowAnyHeader()
                  .AllowAnyMethod()
                  .SetIsOriginAllowed(origin =>
                      Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri) &&
                      (uri.Host == "localhost" ||
                       allowedOrigins.Contains(origin.TrimEnd('/'), StringComparer.OrdinalIgnoreCase) ||
                       (!string.IsNullOrWhiteSpace(previewOriginSuffix) && uri.Scheme == Uri.UriSchemeHttps &&
                        uri.Host.EndsWith(previewOriginSuffix, StringComparison.OrdinalIgnoreCase))));
        });
});
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        string? key = builder.Configuration["Jwt:Key"];

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey =
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key!))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddAutoMapper(cfg => {}, typeof(Program).Assembly, typeof(GymCrest.Application.DIConfig).Assembly);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureServices(builder.Configuration);

builder.Services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(
        c => c.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection")),
        new PostgreSqlStorageOptions
        {
            QueuePollInterval = TimeSpan.FromMinutes(15),
            JobExpirationCheckInterval = TimeSpan.FromHours(12)
        }));

builder.Services.AddHangfireServer(options =>
{
    options.WorkerCount = 1;
    options.SchedulePollingInterval = TimeSpan.FromMinutes(15);
    options.HeartbeatInterval = TimeSpan.FromMinutes(5);
    options.ServerTimeout = TimeSpan.FromMinutes(30);
    options.ServerCheckInterval = TimeSpan.FromMinutes(15);
});

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("OtpPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? httpContext.Request.Headers["X-Forwarded-For"].ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("LoginPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("FoodSearchPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.FindFirst("userId")?.Value ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 15,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.AddPolicy("FeedbackPolicy", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.User.FindFirst("userId")?.Value ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 5,
                Window = TimeSpan.FromMinutes(10),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsJsonAsync(new { message = "Too many requests. Please try again later." }, cancellationToken: token);
    };
});

var app = builder.Build();

// Automatically migrate database on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        Console.WriteLine("Applying pending migrations...");
        context.Database.Migrate();
        Console.WriteLine("Migrations applied successfully.");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

ForwardedHeadersOptions forwardedHeaders = new() { ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto };
forwardedHeaders.KnownIPNetworks.Clear();
forwardedHeaders.KnownProxies.Clear();
app.UseForwardedHeaders(forwardedHeaders);

app.UseMiddleware<ExceptionMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<MaintenanceMiddleware>();
app.UseRateLimiter();

app.UseMiddleware<ResponseWrapperMiddleware>();

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter(app.Configuration, app.Environment) }
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapControllers();

// All times below are IST (Asia/Kolkata) wall-clock times, not UTC - Hangfire's
// Cron.Daily() is UTC by default, so an explicit TimeZone is required or "4:30" silently
// means 4:30 UTC (10:00 IST).
RecurringJobOptions istSchedule = new() { TimeZone = GymCrest.Shared.Helpers.AppTimeZone.India };

// Schedule automated notifications to run every day using Hangfire
RecurringJob.AddOrUpdate<GymCrest.Application.BackgroundJobs.AutomatedNotificationJob>(
    "daily-notifications",
    job => job.ExecuteAsync(),
    Cron.Daily(0, 0), istSchedule); // Midnight IST

RecurringJob.AddOrUpdate<GymCrest.Application.BackgroundJobs.SaaSExpiryJob>(
    "daily-saas-expiry",
    job => job.ExecuteAsync(),
    Cron.Daily(1, 0), istSchedule); // 1:00 AM IST


RecurringJob.AddOrUpdate<GymCrest.Application.BackgroundJobs.MemberAutoUnfreezeJob>(
    "daily-member-auto-unfreeze",
    job => job.ExecuteAsync(),
    Cron.Daily(2, 0), istSchedule); // 2:00 AM IST

app.Run();