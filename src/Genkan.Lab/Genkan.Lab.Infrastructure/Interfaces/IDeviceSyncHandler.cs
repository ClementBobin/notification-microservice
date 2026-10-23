using Genkan.Shared.Messaging.Lab;

namespace Genkan.Lab.Infrastructure.Interfaces;

/// <summary>Implemented by the application layer; the RabbitMQ consumer hands it what the notification service asks for or reports.</summary>
public interface IDeviceSyncHandler
{
    /// <summary>Every device, as the notification service's in-memory copy needs it.</summary>
    Task<DeviceSnapshot> BuildSnapshotAsync(CancellationToken ct);

    /// <summary>Deletes the tokens FCM reported as unregistered and announces <see cref="DevicesRemoved"/> to every notification instance.</summary>
    Task RemoveInvalidAsync(DevicesInvalid message, CancellationToken ct);
}
