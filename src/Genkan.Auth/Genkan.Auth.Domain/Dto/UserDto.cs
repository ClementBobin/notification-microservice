namespace Genkan.Auth.Domain.Dto;

public sealed record UserDto(Guid Id, string Email, string DisplayName, string Role);

public sealed record CreateUserRequest(string Email, string? DisplayName, string Password, string Role);

public sealed record RegisterFcmTokenRequest(string FcmToken);