using Genkan.Shared.Configurations;
using Genkan.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Genkan.Notification.Infrastructure.Data;

/// <summary>Runtime access to the notification tables of the shared schema. Tables and migrations are owned by Genkan.Migrator.</summary>
public class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<NotificationLog> Notifications => Set<NotificationLog>();
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
    public DbSet<DeviceGroupMember> DeviceGroupMembers => Set<DeviceGroupMember>();
    public DbSet<TopicSubscription> TopicSubscriptions => Set<TopicSubscription>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema(GenkanSchema.Name);
        b.ApplyConfiguration(new NotificationLogConfiguration());
        b.ApplyConfiguration(new DeviceTokenConfiguration());
        b.ApplyConfiguration(new DeviceGroupMemberConfiguration());
        b.ApplyConfiguration(new TopicSubscriptionConfiguration());
    }
}
