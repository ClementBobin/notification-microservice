using Genkan.Notification.Application.Interfaces;
using Genkan.Notification.Rabbit;
using Genkan.Notification.Domain.Configuration;

namespace Genkan.Notification.Api.Extensions;

public static class RabbitServiceExtensions
{
    /// <summary>
    /// which hands every received event to <see cref="IEventHandler"/> (registered by the application layer).
    /// </summary>
    public static IServiceCollection AddNotificationRabbit(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<RabbitOptions>(config.GetSection("RabbitMq"));
        services.AddSingleton<RabbitMqConnection>();
        services.AddHostedService<EventConsumer>();
        return services;
    }
}
