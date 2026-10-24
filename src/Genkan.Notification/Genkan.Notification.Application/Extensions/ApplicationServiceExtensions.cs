using Genkan.Notification.Application.Interfaces;
using Genkan.Notification.Application.Services;
using Genkan.Notification.Firebase.Extensions;
using Genkan.Notification.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Genkan.Notification.Application.Extensions;

public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Application layer: the notification log service, the handler for events and the handler for push commands.
    /// It is the only entry point for the hosts (Api, Rabbit): it also wires the layers it calls,
    /// Infrastructure (PostgreSQL) and Firebase (FCM or the logging channel).
    /// </summary>
    public static IServiceCollection AddNotificationApplication(this IServiceCollection services, IConfiguration config)
    {
        services.AddNotificationInfrastructure(config);
        services.AddNotificationFirebase(config);

        services.AddScoped<INotificationService, NotificationService>();

        // Called by the RabbitMQ consumers for every received event / command
        services.AddScoped<IEventHandler, NotificationProcessor>();
        services.AddScoped<IPushCommandHandler, PushCommandHandler>();
        return services;
    }
}
