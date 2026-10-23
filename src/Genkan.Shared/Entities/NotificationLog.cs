namespace Genkan.Shared.Entities;

public class NotificationLog
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public string EventType { get; set; } = "";
    /// <summary>Guid.Empty when <see cref="IsBroadcast"/> (alert to everyone).</summary>
    public Guid RecipientUserId { get; set; }
    public bool IsBroadcast { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public string DataJson { get; set; } = "{}";
    public string Status { get; set; } = NotificationStatus.Pending;
    public string? Error { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
