using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Genkan.Shared.Entities;

[Table("notifications")]
// Idempotence per (event, recipient)
[Index(nameof(EventId), nameof(RecipientUserId), IsUnique = true)]
[Index(nameof(RecipientUserId), nameof(CreatedAt))]
[Index(nameof(CreatedAt))]
public class NotificationLog
{
    [Key]
    public Guid Id { get; set; }

    public Guid EventId { get; set; }

    // Guid.Empty means "broadcast". No FK on purpose: the log must outlive users.
    public Guid RecipientUserId { get; set; }

    [MaxLength(100)]
    public string EventType { get; set; } = "";

    public bool IsBroadcast { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = "";

    [MaxLength(1000)]
    public string Body { get; set; } = "";

    [MaxLength(20)]
    public string Status { get; set; } = NotificationStatus.Pending;

    [MaxLength(500)]
    public string? Error { get; set; }

    [Column(TypeName = "jsonb")]
    public string? DataJson { get; set; }

    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}