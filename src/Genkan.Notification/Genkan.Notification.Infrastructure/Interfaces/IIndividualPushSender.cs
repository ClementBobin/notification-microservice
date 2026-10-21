using Genkan.Notification.Domain.Dto;

namespace Genkan.Notification.Infrastructure.Interfaces;

/// <summary>Sends one message to one device token.</summary>
public interface IIndividualPushSender
{
    Task<PushResult> SendAsync(string token, PushMessage message, CancellationToken ct);
}
