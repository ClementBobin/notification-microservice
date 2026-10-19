using System.Threading.RateLimiting;
using Genkan.Auth.Api;
using Genkan.Auth.Business;
using Genkan.Auth.Data;
using Genkan.Auth.Repository;
using Genkan.Shared;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddJsonConsole(o => o.IncludeScopes = true);

builder.Services.AddDbContext<AuthDbContext>(o => o.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));
builder.Services.AddSingleton<ISessionStore, RedisSessionStore>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<AuthService>();
builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));
builder.Services.AddGatewayHeaderAuth();
builder.Services.AddOpenApi();

// AUTH-08: 10 requests / minute / client IP on login endpoints.
// Behind the gateway the real client is the LAST X-Forwarded-For entry (appended by the gateway).
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("login", ctx =>
    {
        var forwarded = ctx.Request.Headers["X-Forwarded-For"].ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var ip = forwarded.LastOrDefault() ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        });
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await db.EnsureSchemaAsync(AuthDbContext.Schema, "users", app.Logger);

    // First admin account, only when the table is empty.
    var email = app.Configuration["Seed:AdminEmail"];
    var password = app.Configuration["Seed:AdminPassword"];
    var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password) && !await repo.AnyAsync(default))
    {
        var (_, error) = await scope.ServiceProvider.GetRequiredService<AuthService>()
            .CreateUserAsync(email, "Administrator", password, Roles.Admin, default);
        app.Logger.LogInformation("Seeded admin {Email}: {Result}", email, error ?? "ok");
    }
}

app.UseCorrelationId();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapAuthEndpoints();

async Task<IResult> Health(AuthDbContext db, IConnectionMultiplexer redis)
{
    try
    {
        var dbOk = await db.Database.CanConnectAsync();
        await redis.GetDatabase().PingAsync();
        return dbOk ? Results.Ok(new { status = "ok", service = "auth" }) : Results.Json(new { status = "db_down" }, statusCode: 503);
    }
    catch (Exception ex)
    {
        return Results.Json(new { status = "unhealthy", error = ex.Message }, statusCode: 503);
    }
}
app.MapGet("/health", Health);
app.MapGet("/auth/health", Health);

app.Run();
