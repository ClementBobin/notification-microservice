namespace Genkan.Notification.Api.Extensions;

using Genkan.Notification.Application.Interfaces;
using Genkan.Notification.Application.Services;
using Genkan.Notification.Infrastructure.Interfaces;
using Genkan.Notification.Infrastructure.Repositories;

public static class AppServicesExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services,
        IConfiguration config)
    {
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IDeviceTokenRepository, DeviceTokenRepository>();
        services.AddScoped<INotificationService, NotificationService>();

        return services;
    }
}