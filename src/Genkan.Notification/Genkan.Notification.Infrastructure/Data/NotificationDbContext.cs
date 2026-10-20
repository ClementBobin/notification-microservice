using Genkan.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Genkan.Notification.Infrastructure.Data;

public class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public const string Schema = "notification";
    public DbSet<NotificationLog> Notifications => Set<NotificationLog>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<NotificationLog>(e =>
        {
            e.ToTable("notifications", Schema);
            e.HasKey(x => x.Id);
            // Idempotence per (event, recipient)
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

        b.Entity<DeviceToken>(e =>
        {
            e.ToTable("device_tokens", Schema);
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Token).IsUnique();
            e.HasIndex(x => x.UserId);
            e.Property(x => x.Token).HasMaxLength(1024);
            e.Property(x => x.Platform).HasMaxLength(20);
        });
    }
}
