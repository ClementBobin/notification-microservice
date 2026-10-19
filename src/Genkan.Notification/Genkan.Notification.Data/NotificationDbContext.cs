using Genkan.Notification.Entities;
using Microsoft.EntityFrameworkCore;

namespace Genkan.Notification.Data;

public class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public const string Schema = "notification";
    public DbSet<NotificationLog> Notifications => Set<NotificationLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<NotificationLog>(e =>
        {
            e.ToTable("notifications", Schema);
            e.HasKey(x => x.Id);
            // NOTIF-04: idempotence per (event, recipient)
            e.HasIndex(x => new { x.EventId, x.RecipientUserId }).IsUnique();
            e.HasIndex(x => new { x.RecipientUserId, x.CreatedAt });
            e.HasIndex(x => x.CreatedAt);
            e.Property(x => x.EventType).HasMaxLength(100);
            e.Property(x => x.Title).HasMaxLength(200);
            e.Property(x => x.Body).HasMaxLength(1000);
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.Error).HasMaxLength(500);
            e.Property(x => x.DataJson).HasColumnType("jsonb");
        });
    }
}
