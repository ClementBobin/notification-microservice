using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Genkan.Notification.Domain.Configuration;
using Genkan.Notification.Firebase.Interfaces;
using Genkan.Notification.Firebase.Push;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Genkan.Notification.Firebase.Extensions;

public static class FirebaseServiceExtensions
{
    /// <summary>
    /// Firebase:Enabled=true  -> the multicast / topic senders talk to FCM. Credentials come from Application Default
    ///                           Credentials, i.e. the GOOGLE_APPLICATION_CREDENTIALS env var pointing at a service-account JSON file.
    /// Firebase:Enabled=false -> the same interfaces are served by a channel that only logs (local development).
    /// Also binds the public client identifiers served by GET /notifications/firebase-config.
    /// </summary>
    public static IServiceCollection AddNotificationFirebase(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<FirebaseClientOptions>(config.GetSection("Firebase:Client"));

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
            services.AddSingleton<IMulticastPushSender, FcmMulticastPushSender>();
            services.AddSingleton<ITopicPushSender, FcmTopicPushSender>();
            services.AddHostedService<FirebaseStartupValidator>();
        }
        else
        {
            services.AddSingleton<LoggingPushChannel>();
            services.AddSingleton<IMulticastPushSender>(sp => sp.GetRequiredService<LoggingPushChannel>());
            services.AddSingleton<ITopicPushSender>(sp => sp.GetRequiredService<LoggingPushChannel>());
        }

        return services;
    }
}
