namespace Genkan.Shared.Messaging.Lab;

/// <summary>What the push shows on the phone: a title, a text and optional key/value data for the app (the wire counterpart of each service's own PushMessage model).</summary>
public sealed record PushContent(string Title, string Body, IReadOnlyDictionary<string, string>? Data = null);
