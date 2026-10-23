using Genkan.Lab.Infrastructure.Interfaces;
using Genkan.Shared.Messaging.Lab;

namespace Genkan.Lab.Application.Services;

/// <summary>What the notification service asks lab for over RabbitMQ: its initial copy of the devices, and cleanup of dead tokens.</summary>
public sealed class DeviceSyncHandler(IDeviceTokenRepository devices, IMessageBus bus) : IDeviceSyncHandler
{
    public async Task<DeviceSnapshot> BuildSnapshotAsync(CancellationToken ct)
    {
        // Taken before the read: a change made while reading carries a later timestamp, so the live event wins over the snapshot.
        var generatedAt = DateTimeOffset.UtcNow;
        var all = await devices.GetAllAsync(ct);
        return new DeviceSnapshot(generatedAt, all);
    }

    public async Task RemoveInvalidAsync(DevicesInvalid message, CancellationToken ct)
    {
        if (message.Tokens.Count == 0) return;

        await devices.RemoveTokensAsync(message.Tokens.ToList(), ct);
        await bus.PublishAsync(LabTopology.DeviceRemovedKey, new DevicesRemoved(message.Tokens, DateTimeOffset.UtcNow), ct);
    }
}
