using Genkan.Notification.Application.Interfaces;
using Genkan.Notification.Application.Services;

namespace Genkan.Notification.Api.Extensions;

public static class AppServicesExtensions
{
    /// <summary>
    /// Application layer: the notification log service, the handler for events and the handler for push commands.
    /// It is the only entry point for the hosts (Api, Rabbit): it also wires the layers it calls,
    /// Infrastructure (PostgreSQL) and Firebase (FCM or the logging channel).
    /// </summary>
    public static IServiceCollection AddAppServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<INotificationService, NotificationService>();

        // Called by the RabbitMQ consumers for every received event / command
        services.AddScoped<IEventHandler, NotificationProcessor>();
        return services;
    }
}
