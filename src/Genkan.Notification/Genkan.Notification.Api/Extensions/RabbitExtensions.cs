namespace Genkan.Notification.Api.Extensions;

public static class RabbitExtensions
{
    public static IServiceCollection AddRabbit(this IServiceCollection services,
        IConfiguration config)
    {
        // ── RabbitMQ services ───────────────────────────────────────────────
        services.Configure<RabbitOptions>(config.GetSection("RabbitMq"));
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        services.AddScoped<IEventHandler, NotificationProcessor>();

        services.AddHostedService<EventConsumer>();

        return services;
    }
}