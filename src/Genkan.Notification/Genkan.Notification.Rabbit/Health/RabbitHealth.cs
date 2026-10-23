using Genkan.Shared.Health;
using Microsoft.Extensions.DependencyInjection;

namespace Genkan.Notification.Rabbit.Health;

public static class RabbitHealth
{
    public static HealthProbe Probe { get; } =
        new("rabbitmq", async (sp, ct) => (await sp.GetRequiredService<RabbitMqConnection>().GetAsync(ct)).IsOpen);
}
