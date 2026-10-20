using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Genkan.Notification.Infrastructure.Interfaces;
using Genkan.Notification.Infrastructure.Push;

namespace Genkan.Notification.Api.Extensions;

public static class PushServiceCollectionExtensions
{
    /// <summary>
    /// Firebase:Enabled=true  -> the individual / multicast / topic senders talk to FCM. Credentials come from Application Default
    ///                           Credentials, i.e. the GOOGLE_APPLICATION_CREDENTIALS env var pointing at a service-account JSON file.
    /// Firebase:Enabled=false -> the same interfaces are served by a channel that only logs (local development).
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
            services.AddSingleton<IIndividualPushSender, FcmIndividualPushSender>();
            services.AddSingleton<IMulticastPushSender, FcmMulticastPushSender>();
            services.AddSingleton<ITopicPushSender, FcmTopicPushSender>();
        }
        else
        {
            services.AddSingleton<LoggingPushChannel>();
            services.AddSingleton<IIndividualPushSender>(sp => sp.GetRequiredService<LoggingPushChannel>());
            services.AddSingleton<IMulticastPushSender>(sp => sp.GetRequiredService<LoggingPushChannel>());
            services.AddSingleton<ITopicPushSender>(sp => sp.GetRequiredService<LoggingPushChannel>());
        }

        // Push used for the events consumed from RabbitMQ
        services.AddScoped<IPushSender, NotificationPushSender>();
        return services;
    }
}
