using Genkan.Auth.Domain.Dto;

namespace Genkan.Auth.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResult?> LoginAsync(string email, string password, CancellationToken ct);
    Task<SessionInfo?> GetSessionAsync(string token);
    Task LogoutAsync(string token);
}
