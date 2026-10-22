using Genkan.Shared.Configurations;
using Genkan.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Genkan.Lab.Infrastructure.Data;

/// <summary>Runtime access to the lab tables (devices, device groups, topic subscriptions). Tables and migrations are owned by Genkan.Migrator.</summary>
public class LabDbContext(DbContextOptions<LabDbContext> options) : DbContext(options)
{
    public DbSet<DeviceToken> DeviceTokens => Set<DeviceToken>();
    public DbSet<DeviceGroupMember> DeviceGroupMembers => Set<DeviceGroupMember>();
    public DbSet<TopicSubscription> TopicSubscriptions => Set<TopicSubscription>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.ApplyConfiguration(new DeviceTokenConfiguration());
        b.ApplyConfiguration(new DeviceGroupMemberConfiguration());
        b.ApplyConfiguration(new TopicSubscriptionConfiguration());
    }
}
