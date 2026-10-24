using System.Text.Json;

namespace Genkan.Shared.Messaging;

/// <summary>The JSON settings used by every message on RabbitMQ (camelCase, the web defaults), so publisher and consumer always read each other.</summary>
public static class EventJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
