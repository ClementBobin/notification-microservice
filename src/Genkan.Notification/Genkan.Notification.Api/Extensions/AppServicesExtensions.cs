using Genkan.Notification.Application.Interfaces;
using Genkan.Notification.Application.Services;
using Genkan.Notification.Infrastructure.Interfaces;

namespace Genkan.Notification.Api.Extensions;

public static class AppServicesExtensions
{
    /// <summary>Application layer: one service per kind of send, plus the handler for events received from RabbitMQ.</summary>
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IIndividualPushService, IndividualPushService>();
        services.AddScoped<IMulticastPushService, MulticastPushService>();
        services.AddScoped<IDeviceGroupPushService, DeviceGroupPushService>();
        services.AddScoped<ITopicPushService, TopicPushService>();

        // Called by the RabbitMQ consumer for every received event
        services.AddScoped<IEventHandler, NotificationProcessor>();
        return services;
    }
}
