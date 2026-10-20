namespace Genkan.Auth.Domain.Dto;

public sealed record SessionInfo(Guid UserId, string Email, string Role, string DisplayName);
