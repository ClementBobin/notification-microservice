using System.Threading.RateLimiting;
using Genkan.Auth.Application.Interfaces;
using Genkan.Auth.Application.Services;
using Genkan.Auth.Infrastructure.Data;
using Genkan.Auth.Infrastructure.Interfaces;
using Genkan.Auth.Infrastructure.Repositories;
using Genkan.Shared;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddJsonConsole(o => o.IncludeScopes = true);

builder.Services.Configure<AuthOptions>(builder.Configuration.GetSection("Auth"));
builder.Services.AddGatewayHeaderAuth();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

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
        var (_, error) = await scope.ServiceProvider.GetRequiredService<IAuthService>()
            .CreateUserAsync(email, "Administrator", password, Roles.Admin, default);
        app.Logger.LogInformation("Seeded admin {Email}: {Result}", email, error ?? "ok");
    }
}

app.UseCorrelationId();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapControllers();

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
