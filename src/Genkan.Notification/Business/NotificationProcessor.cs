using System.Text.Json;
using Genkan.Notification.Entities;
using Genkan.Notification.Repository;
using Genkan.Shared;

namespace Genkan.Notification.Business;

public sealed record NotificationContent(string Title, string Body);

/// <summary>NOTIF-02: event type -> title/body. Unknown types return null and are ignored.</summary>
public static class NotificationTemplates
{
    private static string S(EventEnvelope e, string name) =>
        e.Data.ValueKind == JsonValueKind.Object && e.Data.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String
            ? v.GetString() ?? "" : "";

    public static NotificationContent? Build(EventEnvelope e) => e.Type switch
    {
        "booking.created" => new("Room booked", $"{S(e, "room")} · {S(e, "start")}"),
        "booking.cancelled" => new("Booking cancelled", S(e, "room")),
        "access.denied" => new("Access denied", S(e, "door")),
        "visitor.checked-in" => new($"{S(e, "visitor")} has arrived", "Waiting at reception"),
        "alert.broadcast" => new(S(e, "title"), S(e, "message")),
        _ => null,
    };
}

public interface IPushSender
{
    /// <summary>Delivers the notification to the recipient's devices. Throw on failure.</summary>
    Task SendAsync(NotificationLog notification, CancellationToken ct);
}

/// <summary>Placeholder until FCM is wired: only logs. Replace with a Firebase implementation of IPushSender.</summary>
public sealed class LoggingPushSender(ILogger<LoggingPushSender> log) : IPushSender
{
    public Task SendAsync(NotificationLog n, CancellationToken ct)
    {
        log.LogInformation("PUSH [{Type}] to {Recipient}: {Title} - {Body}", n.EventType, n.IsBroadcast ? "everyone" : n.RecipientUserId, n.Title, n.Body);
        return Task.CompletedTask;
    }
}

public sealed class NotificationProcessor(INotificationRepository repo, IPushSender push, ILogger<NotificationProcessor> log)
{
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
                await push.SendAsync(n, ct);
                await repo.UpdateStatusAsync(n.Id, NotificationStatus.Sent, null, ct);
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
