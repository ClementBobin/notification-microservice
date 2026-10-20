namespace Genkan.Notification.Domain.Entities;

/// <summary>
/// A user's subscription to an FCM topic. Kept so that a rotated or newly registered token can be re-subscribed
/// (FCM ties topic subscriptions to the token, not to the user).
/// </summary>
public class TopicSubscription
{
    public Guid Id { get; set; }
    public string Topic { get; set; } = "";
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
