using FirebaseAdmin.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Genkan.Notification.Firebase;

/// <summary>
/// Fails at startup when Firebase is enabled but the credentials are missing or invalid,
/// instead of dead-lettering every RabbitMQ event later. Registered only when Firebase:Enabled=true.
/// </summary>
public sealed class FirebaseStartupValidator(IServiceProvider services) : IHostedService
{
    public Task StartAsync(CancellationToken ct)
    {
        services.GetRequiredService<FirebaseMessaging>();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
