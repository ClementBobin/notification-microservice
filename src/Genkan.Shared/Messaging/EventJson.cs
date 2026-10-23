using System.Text.Json;

namespace Genkan.Shared.Messaging;

public static class EventJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
