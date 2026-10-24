using Genkan.Auth.Infrastructure.Data;
using Genkan.Shared.Health;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace Genkan.Auth.Api.Extensions;

public static class HealthExtensions
{
    public static WebApplication MapAuthHealth(this WebApplication app)
    {
        app.MapServiceHealth("auth", "auth",
            new HealthProbe("database", async (sp, ct) => await sp.GetRequiredService<AuthDbContext>().Database.CanConnectAsync(ct)),
            new HealthProbe("redis", async (sp, ct) =>
            {
                await sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase().PingAsync();
                return true;
            }));
        return app;
    }
}
