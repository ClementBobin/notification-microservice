using Genkan.Notification.Application.Interfaces;
using Genkan.Notification.Application.Services;
using Genkan.Notification.Infrastructure.Interfaces;

namespace Genkan.Notification.Api.Extensions;

public static class AppServicesExtensions
{
    /// <summary>Application layer: the notification log service, the handler for events and the handler for push commands received from RabbitMQ.</summary>
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();

        // Called by the RabbitMQ consumers for every received event / command
        services.AddScoped<IEventHandler, NotificationProcessor>();
        services.AddScoped<IPushCommandHandler, PushCommandHandler>();
        return services;
    }
}
