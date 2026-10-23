using Genkan.Notification.Infrastructure.Data;
using Genkan.Notification.Infrastructure.Interfaces;
using Genkan.Notification.Infrastructure.Messaging;

namespace Genkan.Notification.Api.Extensions;

public static class RabbitExtensions
{
    /// <summary>
    /// RabbitMQ connection and event publisher, the consumer of the notification queue, and the link with the lab service:
    /// the in-memory device cache + its sync, the consumer of push commands, and the bus used to report dead tokens.
    /// </summary>
    public static IServiceCollection AddRabbitServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<RabbitOptions>(config.GetSection("RabbitMq"));
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        services.AddHostedService<EventConsumer>();
        return services;
    }
}
