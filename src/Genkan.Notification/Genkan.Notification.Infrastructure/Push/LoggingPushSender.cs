using Genkan.Notification.Domain.Entities;
using Genkan.Notification.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;

namespace Genkan.Notification.Infrastructure.Push;

/// <summary>Used when Firebase:Enabled is false (local dev): only logs, reports one device reached.</summary>
public sealed class LoggingPushSender(ILogger<LoggingPushSender> log) : IPushSender
{
    public Task<int> SendAsync(NotificationLog n, CancellationToken ct)
    {
        log.LogInformation("PUSH [{Type}] to {Recipient}: {Title} - {Body}", n.EventType, n.IsBroadcast ? "everyone" : n.RecipientUserId, n.Title, n.Body);
        return Task.FromResult(1);
    }
}
