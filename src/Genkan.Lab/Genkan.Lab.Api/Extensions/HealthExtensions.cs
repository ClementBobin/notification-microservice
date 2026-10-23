using Genkan.Lab.Infrastructure.Data;
using Genkan.Lab.Infrastructure.Messaging;
using Genkan.Shared.Health;
using Microsoft.EntityFrameworkCore;

namespace Genkan.Lab.Api.Extensions;

public static class HealthExtensions
{
    public static WebApplication MapLabHealth(this WebApplication app)
    {
        app.MapServiceHealth("lab", "lab",
            new HealthProbe("database", async (sp, ct) => await sp.GetRequiredService<LabDbContext>().Database.CanConnectAsync(ct)),
            new HealthProbe("rabbitmq", async (sp, ct) => (await sp.GetRequiredService<RabbitMqConnection>().GetAsync(ct)).IsOpen));
        return app;
    }
}
