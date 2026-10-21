using Genkan.Notification.Infrastructure.Interfaces;
using Genkan.Notification.Infrastructure.Messaging;

namespace Genkan.Notification.Api.Extensions;

public static class RabbitExtensions
{
    /// <summary>RabbitMQ connection, event publisher and the background consumer of the notification queue.</summary>
    public static IServiceCollection AddRabbitServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<RabbitOptions>(config.GetSection("RabbitMq"));
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        services.AddHostedService<EventConsumer>();
        return services;
    }
}
