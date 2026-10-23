namespace Genkan.Shared.Messaging.Lab;

/// <summary>What a push shows on the device (the wire counterpart of each service's own PushMessage model).</summary>
public sealed record PushContent(string Title, string Body, IReadOnlyDictionary<string, string>? Data = null);
