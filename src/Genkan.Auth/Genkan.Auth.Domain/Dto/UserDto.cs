namespace Genkan.Auth.Domain.Dto;

public sealed record UserDto(Guid Id, string Email, string DisplayName, string Role);
