using Genkan.Auth.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Genkan.Auth.Infrastructure.Data;

public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public const string Schema = "auth";
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.ToTable("users", Schema);
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.Property(x => x.Email).HasMaxLength(320);
            e.Property(x => x.DisplayName).HasMaxLength(200);
            e.Property(x => x.Role).HasMaxLength(20);
        });
    }
}
