using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Genkan.Notification.Application.Interfaces;

namespace Genkan.Notification.Rabbit.Extensions;

public static class RabbitServiceExtensions
{
    /// <summary>
    /// RabbitMQ connection, the event publisher (<see cref="IEventPublisher"/>) and the consumer of the notification queue,
    /// which hands every received event to <see cref="IEventHandler"/> (registered by the application layer).
    /// </summary>
    public static IServiceCollection AddNotificationRabbit(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<RabbitOptions>(config.GetSection("RabbitMq"));
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        services.AddHostedService<EventConsumer>();
        return services;
    }
}
