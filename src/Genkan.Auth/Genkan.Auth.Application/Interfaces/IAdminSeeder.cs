namespace Genkan.Auth.Application.Interfaces;

public interface IAdminSeeder
{
    /// <summary>Creates the configured administrator when the user table is empty. Does nothing otherwise.</summary>
    Task SeedAsync(CancellationToken ct);
}
