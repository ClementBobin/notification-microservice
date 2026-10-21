using Genkan.Shared.Entities;

namespace Genkan.Notification.Infrastructure.Interfaces;

public interface IPushSender
{
    /// <summary>Delivers the notification to the recipient devices. Returns how many devices were reached (0 = nothing to deliver to). Throws when delivery failed.</summary>
    Task<int> SendAsync(NotificationLog notification, CancellationToken ct);
}
