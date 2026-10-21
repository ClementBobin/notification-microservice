namespace Genkan.Notification.Domain.Dto;

public sealed record CreateAlertRequest(string Title, string Message);

public sealed record RegisterDeviceRequest(string Token, string Platform);

public sealed record UnregisterDeviceRequest(string Token);
