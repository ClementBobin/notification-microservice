namespace Genkan.Shared.Messaging.Lab;

/// <summary>
/// Routing key <see cref="LabTopology.DeviceInvalidKey"/> (<c>device.invalid</c>). Notification -> lab.
/// While sending a push, FCM answered that these tokens are unregistered (app uninstalled, data cleared...). Notification cannot delete
/// them itself because lab owns the devices: lab deletes them from the database, then announces <see cref="DevicesRemoved"/> to everyone.
/// </summary>
/// <param name="Tokens">The tokens FCM rejected.</param>
/// <param name="At">When notification received the FCM answer (notification clock).</param>
public sealed record DevicesInvalid(IReadOnlyList<string> Tokens, DateTimeOffset At);
