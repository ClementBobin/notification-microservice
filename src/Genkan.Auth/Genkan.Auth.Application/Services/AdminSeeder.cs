using Genkan.Auth.Application.Interfaces;
using Genkan.Auth.Infrastructure.Interfaces;
using Genkan.Shared;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Genkan.Auth.Application.Services;

public sealed class AdminSeeder(IUserRepository users, IUserService userService, IOptions<SeedOptions> options, ILogger<AdminSeeder> log) : IAdminSeeder
{
    public async Task SeedAsync(CancellationToken ct)
    {
        var o = options.Value;
        if (string.IsNullOrWhiteSpace(o.AdminEmail) || string.IsNullOrWhiteSpace(o.AdminPassword)) return;
        if (await users.AnyAsync(ct)) return;

        var (_, error) = await userService.CreateUserAsync(o.AdminEmail, "Administrator", o.AdminPassword, Roles.Admin, ct);
        log.LogInformation("Seeded admin {Email}: {Result}", o.AdminEmail, error ?? "ok");
    }
}
