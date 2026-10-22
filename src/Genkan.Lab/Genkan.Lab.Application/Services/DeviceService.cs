using Genkan.Lab.Application.Interfaces;
using Genkan.Lab.Infrastructure.Interfaces;
using Genkan.Shared;
using Microsoft.Extensions.Logging;

namespace Genkan.Lab.Application.Services;

/// <summary>
/// Device registration. The database is the source of truth; the notification service learns about every change over RabbitMQ.
/// Announcing is best effort: when the broker is down the change is still saved and the notification service
/// catches up at its next snapshot (see the LabSync settings of the notification service).
/// </summary>
public sealed class DeviceService(IDeviceTokenRepository devices, ITopicSubscriptionRepository subscriptions, IMessageBus bus, ILogger<DeviceService> log) : IDeviceService
{
    public async Task RegisterAsync(Guid userId, string token, string platform, CancellationToken ct)
    {
        await devices.UpsertAsync(userId, token, platform, ct);

        try
        {
            var now = DateTimeOffset.UtcNow;
            await bus.PublishAsync(LabTopology.DeviceRegisteredKey, new DeviceRegistered(userId, token, platform, now), ct);

            // FCM ties topic subscriptions to the token: ask for this one to be subscribed to the topics of its user.
            foreach (var topic in await subscriptions.GetTopicsAsync(userId, ct))
                await bus.PublishAsync(LabTopology.TopicSubscriptionKey, new TopicSubscriptionCommand(Guid.NewGuid(), topic, [token], true, now), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogWarning(ex, "Device of user {UserId} saved but not announced on RabbitMQ", userId);
        }
    }

    public async Task<bool> UnregisterAsync(Guid userId, string token, CancellationToken ct)
    {
        var removed = await devices.RemoveAsync(userId, token, ct);
        if (!removed) return false;

        try
        {
            await bus.PublishAsync(LabTopology.DeviceRemovedKey, new DevicesRemoved([token], DateTimeOffset.UtcNow), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogWarning(ex, "Device of user {UserId} removed but not announced on RabbitMQ", userId);
        }
        return true;
    }
}
