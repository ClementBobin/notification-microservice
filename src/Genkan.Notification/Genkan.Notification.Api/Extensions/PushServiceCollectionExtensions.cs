using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Genkan.Notification.Infrastructure.Interfaces;
using Genkan.Notification.Infrastructure.Push;

namespace Genkan.Notification.Api.Extensions;

public static class PushServiceCollectionExtensions
{
    /// <summary>
    /// Firebase:Enabled=true  -> push through FCM. Credentials come from Application Default Credentials,
    ///                           i.e. the GOOGLE_APPLICATION_CREDENTIALS env var pointing at a service-account JSON file.
    /// Firebase:Enabled=false -> log only (local development).
    /// </summary>
    public static IServiceCollection AddPushSender(this IServiceCollection services, IConfiguration config)
    {
        if (config.GetValue<bool>("Firebase:Enabled"))
        {
            services.AddSingleton(_ =>
            {
                var projectId = config["Firebase:ProjectId"];
                var app = FirebaseApp.DefaultInstance ?? FirebaseApp.Create(new AppOptions
                {
                    ProjectId = string.IsNullOrWhiteSpace(projectId) ? null : projectId,
                });
                return FirebaseMessaging.GetMessaging(app);
            });
            services.AddScoped<IPushSender, FcmPushSender>();
        }
        else
        {
            services.AddScoped<IPushSender, LoggingPushSender>();
        }
        return services;
    }
}
