using Genkan.Shared.Entities;
using Genkan.Shared.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Genkan.Migrator;

/// <summary>
/// The complete model of the shared schema. Only the migrator uses it, so it is the only place that knows the
/// relationships between tables of different services (foreign keys). Migrations are generated from this context.
/// </summary>
public sealed class GenkanDbContext(DbContextOptions<GenkanDbContext> options) : DbContext(options)
{
    /// <summary>Single place where the context is configured (runtime and design time).</summary>
    public static void Configure(DbContextOptionsBuilder options, string? connectionString) =>
        options.UseNpgsql(connectionString, npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", GenkanSchema.Name));

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema(GenkanSchema.Name);

        b.ApplyConfiguration(new UserConfiguration());
        b.ApplyConfiguration(new NotificationLogConfiguration());
        b.ApplyConfiguration(new DeviceTokenConfiguration());
        b.ApplyConfiguration(new DeviceGroupMemberConfiguration());
        b.ApplyConfiguration(new TopicSubscriptionConfiguration());

        // Cross-service foreign keys: removing a user removes their devices, group memberships and topic subscriptions.
        // The services' own contexts do not know these relationships; the database enforces them.
        b.Entity<DeviceToken>().HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<DeviceGroupMember>().HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        b.Entity<TopicSubscription>().HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        // NotificationLog.RecipientUserId has no FK on purpose: Guid.Empty means "broadcast" and the log must outlive users.
    }
}
