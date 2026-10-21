using Genkan.Shared.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Genkan.Shared.Configurations;

public sealed class NotificationLogConfiguration : IEntityTypeConfiguration<NotificationLog>
{
    public void Configure(EntityTypeBuilder<NotificationLog> e)
    {
        e.ToTable("notifications");
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
    }
}
