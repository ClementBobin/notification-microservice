namespace Genkan.Auth.Domain.Dto;

public sealed record LoginResult(string Token, DateTimeOffset ExpiresAt, UserDto User);
