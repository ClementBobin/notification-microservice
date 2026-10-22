using Genkan.Lab.Application.Interfaces;
using Genkan.Lab.Application.Services;
using Genkan.Lab.Infrastructure.Interfaces;

namespace Genkan.Lab.Api.Extensions;

public static class AppServicesExtensions
{
    /// <summary>Application layer: device registration, one service per kind of push, and the handler of what notification asks over RabbitMQ.</summary>
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<IDeviceService, DeviceService>();
        services.AddScoped<IIndividualPushService, IndividualPushService>();
        services.AddScoped<IMulticastPushService, MulticastPushService>();
        services.AddScoped<IDeviceGroupPushService, DeviceGroupPushService>();
        services.AddScoped<ITopicPushService, TopicPushService>();
        services.AddScoped<IDeviceSyncHandler, DeviceSyncHandler>();
        return services;
    }
}
