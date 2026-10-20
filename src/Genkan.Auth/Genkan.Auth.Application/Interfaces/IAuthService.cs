using Genkan.Auth.Domain.Dto;

namespace Genkan.Auth.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResult?> LoginAsync(string email, string password, CancellationToken ct);
    Task<SessionInfo?> GetSessionAsync(string token);
    Task LogoutAsync(string token);
    Task<(UserDto? User, string? Error)> CreateUserAsync(string email, string? displayName, string password, string role, CancellationToken ct);
}
