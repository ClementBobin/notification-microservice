using Genkan.Migrator.Configuration;
using Genkan.Shared.Auth;
using Genkan.Shared.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Genkan.Migrator;

public sealed class Seeder(GenkanDbContext db, IOptions<SeedOptions> options, ILogger<Seeder> log)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.AdminEmail) || string.IsNullOrWhiteSpace(o.AdminPassword))
        {
            log.LogInformation("No admin seed configured, skipping");
            return;
        }

        if (await db.Set<User>().AnyAsync(ct))
        {
            log.LogInformation("Users already exist, skipping seed");
            return;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = o.AdminEmail.Trim().ToLowerInvariant(),
            DisplayName = string.IsNullOrWhiteSpace(o.AdminDisplayName) ? "Administrator" : o.AdminDisplayName,
            Role = Roles.Admin,
            CreatedAt = DateTimeOffset.UtcNow
        };
        user.PasswordHash = PasswordHasher.Hash(o.AdminPassword);

        db.Add(user);
        await db.SaveChangesAsync(ct);
        log.LogInformation("Seeded admin {Email}", user.Email);
    }
}