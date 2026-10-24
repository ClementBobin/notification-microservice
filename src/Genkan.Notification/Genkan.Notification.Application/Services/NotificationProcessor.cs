using System.Text.Json;
using Genkan.Notification.Application.Interfaces;
using Genkan.Notification.Domain;
using Genkan.Notification.Domain.Dto;
using Genkan.Notification.Domain.Mapping;
using Genkan.Notification.Firebase.Interfaces;
using Genkan.Notification.Infrastructure.Interfaces;
using Genkan.Shared.Messaging;
using Genkan.Shared.Entities;
using Microsoft.Extensions.Logging;

namespace Genkan.Notification.Application.Services;

/// <summary>
/// Handles every event received from RabbitMQ: builds the notification, stores it (idempotent per event + recipient)
/// and pushes it through <see cref="IMulticastPushSender"/> (Firebase Cloud Messaging in production).
/// </summary>
public sealed class NotificationProcessor(INotificationRepository repo, IMulticastPushSender push, ILogger<NotificationProcessor> log) : IEventHandler
{
    public Task HandleAsync(EventEnvelope e, CancellationToken ct) => ProcessAsync(e, ct);

    public async Task<int> ProcessAsync(EventEnvelope e, CancellationToken ct)
    {
        var content = NotificationTemplates.Build(e);
        if (content is null)
        {
            log.LogInformation("Ignoring unknown event type {Type}", e.Type);
            return 0;
        }

        var recipients = (e.RecipientUserIds ?? []).Distinct().ToList();
        if (recipients.Count == 0 && e.Type.StartsWith("alert.", StringComparison.Ordinal))
            recipients.Add(Guid.Empty); // broadcast
        if (recipients.Count == 0)
        {
            log.LogWarning("Event {EventId} ({Type}) has no recipients", e.EventId, e.Type);
            return 0;
        }

        var created = 0;
        foreach (var recipient in recipients)
        {
            var n = new NotificationLog
            {
                Id = Guid.NewGuid(),
                EventId = e.EventId,
                EventType = e.Type,
                RecipientUserId = recipient,
                IsBroadcast = recipient == Guid.Empty,
                Title = content.Title,
                Body = content.Body,
                DataJson = e.Data.ValueKind == JsonValueKind.Undefined ? "{}" : e.Data.GetRawText(),
                Status = NotificationStatus.Pending,
                OccurredAt = e.OccurredAt.ToUniversalTime(),
                CreatedAt = DateTimeOffset.UtcNow,
            };

            if (!await repo.TryAddAsync(n, ct))
            {
                log.LogInformation("Duplicate delivery of event {EventId} for {Recipient}, skipped", e.EventId, recipient);
                continue;
            }

            try
            {
                var message = n.ToPushMessage();
                if (!message.IsValid()) return 0;
                PushResult reached = await push.SendAsync(["0"], n.ToPushMessage(), ct);
                if (!reached.IsEmpty()) await repo.UpdateStatusAsync(n.Id, NotificationStatus.Sent, null, ct);
                else await repo.UpdateStatusAsync(n.Id, NotificationStatus.Skipped, reached.Error, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                log.LogError(ex, "Push failed for notification {Id}", n.Id);
                await repo.UpdateStatusAsync(n.Id, NotificationStatus.Failed, ex.Message.Length > 500 ? ex.Message[..500] : ex.Message, ct);
            }
            created++;
        }
        return created;
    }
}
