using Genkan.Shared.Entities;

namespace Genkan.Auth.Infrastructure.Interfaces;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
    Task RegisterFcmTokenAsync(Guid userId, string fcmToken, CancellationToken ct);
}
