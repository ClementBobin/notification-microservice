
namespace Genkan.Notification.Domain.Dto;

public sealed record NotificationDto(
    Guid Id, Guid EventId, string Type, Guid? RecipientUserId, bool IsBroadcast,
    string Title, string Body, string Status, string? Error, DateTimeOffset OccurredAt, DateTimeOffset CreatedAt);

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);

public sealed record NotificationContent(string Title, string Body);

public sealed record NotificationFilter(Guid? UserId, bool IncludeBroadcast, string? Type, string? Status, int Page, int PageSize);
