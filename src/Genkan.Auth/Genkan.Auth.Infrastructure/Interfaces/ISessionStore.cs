using Genkan.Auth.Domain.Dto;

namespace Genkan.Auth.Infrastructure.Interfaces;

public interface ISessionStore
{
    Task<string> CreateSessionAsync(SessionInfo info, TimeSpan ttl);
    Task<SessionInfo?> GetSessionAsync(string token);
    Task DeleteSessionAsync(string token);
}
