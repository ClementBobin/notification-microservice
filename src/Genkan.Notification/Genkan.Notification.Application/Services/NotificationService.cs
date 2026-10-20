using System.Text.Json;
using Genkan.Notification.Application.Interfaces;
using Genkan.Notification.Domain.Dto;
using Genkan.Notification.Domain.Entities;
using Genkan.Notification.Infrastructure.Interfaces;
using Genkan.Shared;
using Microsoft.Extensions.Logging;

namespace Genkan.Notification.Application.Services;

public sealed class NotificationService(
    INotificationRepository repo, IDeviceTokenRepository devices, IEventPublisher publisher,
    ITopicPushService topics, ILogger<NotificationService> log) : INotificationService
{
    private static NotificationDto ToDto(NotificationLog n) => new(
        n.Id, n.EventId, n.EventType, n.IsBroadcast ? null : n.RecipientUserId, n.IsBroadcast,
        n.Title, n.Body, n.Status, n.Error, n.OccurredAt, n.CreatedAt);

    public async Task<PagedResult<NotificationDto>> GetLogsAsync(Guid? userId, bool includeBroadcast, string? type, string? status, int? page, int? pageSize, CancellationToken ct)
    {
        var p = Math.Max(page ?? 1, 1);
        var size = Math.Clamp(pageSize ?? 25, 1, 100);
        var (items, total) = await repo.QueryAsync(new NotificationFilter(userId, includeBroadcast, type, status, p, size), ct);
        return new PagedResult<NotificationDto>(items.Select(ToDto).ToList(), total, p, size);
    }

    /// <summary>Publishes an alert.broadcast event; the consumer turns it into a notification for everyone.</summary>
    public async Task<Guid> CreateAlertAsync(string title, string message, Guid createdBy, CancellationToken ct)
    {
        var envelope = new EventEnvelope(
            Guid.NewGuid(), "alert.broadcast", DateTimeOffset.UtcNow, [],
            JsonSerializer.SerializeToElement(new { title, message, createdBy }, EventJson.Options));
        await publisher.PublishAsync(envelope, ct);
        return envelope.EventId;
    }

    public async Task RegisterDeviceAsync(Guid userId, string token, string platform, CancellationToken ct)
    {
        await devices.UpsertAsync(userId, token, platform, ct);

        // FCM ties topic subscriptions to the token: re-subscribe this one (best effort, never fails the registration).
        try
        {
            await topics.SubscribeDeviceToUserTopicsAsync(userId, token, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            log.LogWarning(ex, "Could not re-subscribe the device of user {UserId} to its topics", userId);
        }
    }

    public Task<bool> UnregisterDeviceAsync(Guid userId, string token, CancellationToken ct) =>
        devices.RemoveAsync(userId, token, ct);
}
