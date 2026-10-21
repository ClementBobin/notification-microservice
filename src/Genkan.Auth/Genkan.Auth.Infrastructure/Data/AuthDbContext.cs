using Genkan.Shared.Entities;
using Genkan.Shared.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Genkan.Auth.Infrastructure.Data;

/// <summary>Runtime access to the auth tables. Tables and migrations are owned by Genkan.Migrator.</summary>
public class AuthDbContext(DbContextOptions<AuthDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.ApplyConfiguration(new UserConfiguration());
    }
}
