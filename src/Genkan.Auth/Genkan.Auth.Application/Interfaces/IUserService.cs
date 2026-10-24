using Genkan.Auth.Domain.Dto;

namespace Genkan.Auth.Application.Interfaces;

public interface IUserService
{
    Task<(UserDto? User, string? Error)> CreateUserAsync(string email, string? displayName, string password, string role, CancellationToken ct);
    Task<string?> RegisterFcmTokenAsync(string token, string fcmToken, CancellationToken ct);
}
