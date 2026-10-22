using Genkan.Lab.Infrastructure.Interfaces;
using Genkan.Lab.Infrastructure.Messaging;

namespace Genkan.Lab.Api.Extensions;

public static class RabbitExtensions
{
    /// <summary>RabbitMQ connection, the publisher used by the services, and the background consumer of the lab queue.</summary>
    public static IServiceCollection AddRabbitServices(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<RabbitOptions>(config.GetSection("RabbitMq"));
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IMessageBus, RabbitMqMessageBus>();
        services.AddHostedService<LabConsumer>();
        return services;
    }
}
