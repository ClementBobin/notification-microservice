namespace Genkan.Lab.Application.Interfaces;

public interface IDeviceService
{
    /// <summary>Stores the device token of the user and announces it (and its topic subscriptions) to the notification service.</summary>
    Task RegisterAsync(Guid userId, string token, string platform, CancellationToken ct);

    /// <summary>Returns false when the user has no such token.</summary>
    Task<bool> UnregisterAsync(Guid userId, string token, CancellationToken ct);
}
