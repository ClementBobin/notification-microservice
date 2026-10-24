using System.Text.Json;
using Genkan.Notification.Domain.Dto;
using Genkan.Shared.Messaging;

namespace Genkan.Notification.Domain;

/// <summary>Event type -> title/body. Unknown types return null and are ignored.</summary>
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
