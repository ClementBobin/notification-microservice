using Genkan.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Genkan.Migrator;

/// <summary>
/// The complete model of the shared database. Only the migrator uses it, so it is the only place that knows the
/// relationships between tables of different services (foreign keys). Migrations are generated from this context.
/// </summary>
public sealed class GenkanDbContext(DbContextOptions<GenkanDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<NotificationLog> Notifications => Set<NotificationLog>();

    /// <summary>Single place where the context is configured (runtime and design time).</summary>
    public static void Configure(DbContextOptionsBuilder options, string? connectionString) =>
        options.UseNpgsql(connectionString); // no schema imposed: tables and __EFMigrationsHistory go where the connection string points (Search Path, default "public")
}