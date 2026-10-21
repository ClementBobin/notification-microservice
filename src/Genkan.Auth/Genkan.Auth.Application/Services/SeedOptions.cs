namespace Genkan.Auth.Application.Services;

/// <summary>First administrator, created at startup only when there is no user yet (section "Seed").</summary>
public sealed class SeedOptions
{
    public string AdminEmail { get; set; } = "";
    public string AdminPassword { get; set; } = "";
}
