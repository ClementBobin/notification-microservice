using Genkan.Shared.Entities;
using Microsoft.EntityFrameworkCore;

namespace Genkan.Notification.Infrastructure.Data;

/// <summary>Runtime access to the notification log table (devices, groups and topics belong to the lab service). Tables and migrations are owned by Genkan.Migrator.</summary>
public class NotificationDbContext(DbContextOptions<NotificationDbContext> options) : DbContext(options)
{
    public DbSet<NotificationLog> Notifications => Set<NotificationLog>();
}
