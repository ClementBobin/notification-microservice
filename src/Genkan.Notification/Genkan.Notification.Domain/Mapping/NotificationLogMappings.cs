using System.Text.Json;
using Genkan.Notification.Domain.Dto;
using Genkan.Shared.Entities;

namespace Genkan.Notification.Domain.Mapping;

public static class NotificationLogMappings
{
    public static PushMessage ToPushMessage(this NotificationLog n) =>
        new(n.Title, n.Body, BuildData(n));

    // FCM "data" only accepts string -> string, so the jsonb payload is flattened:
    // strings as-is, everything else (numbers, bools, nested objects/arrays) as raw JSON text.
    private static Dictionary<string, string> BuildData(NotificationLog n)
    {
        var data = new Dictionary<string, string>();

        if (!string.IsNullOrWhiteSpace(n.DataJson))
        {
            try
            {
                using var doc = JsonDocument.Parse(n.DataJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Object)
                {
                    foreach (var p in doc.RootElement.EnumerateObject())
                    {
                        // FCM reserves these prefixes
                        if (p.Name.StartsWith("google", StringComparison.OrdinalIgnoreCase) ||
                            p.Name.StartsWith("gcm", StringComparison.OrdinalIgnoreCase) ||
                            p.Name.Equals("from", StringComparison.OrdinalIgnoreCase))
                            continue;

                        switch (p.Value.ValueKind)
                        {
                            case JsonValueKind.Null:
                            case JsonValueKind.Undefined:
                                break;
                            case JsonValueKind.String:
                                data[p.Name] = p.Value.GetString() ?? "";
                                break;
                            default:
                                data[p.Name] = p.Value.GetRawText();
                                break;
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // malformed payload: push the title/body anyway
            }
        }

        // Set last so the payload can't override them
        data["notificationId"] = n.Id.ToString();
        data["eventId"] = n.EventId.ToString();
        data["eventType"] = n.EventType;

        return data;
    }
}