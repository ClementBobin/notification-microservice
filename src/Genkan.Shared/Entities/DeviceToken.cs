namespace Genkan.Shared.Entities;

/// <summary>An FCM registration token of one device belonging to one user.</summary>
public class DeviceToken
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Token { get; set; } = "";
    /// <summary>android | ios | web</summary>
    public string Platform { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
}
