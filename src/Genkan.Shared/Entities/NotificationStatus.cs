namespace Genkan.Shared.Entities;

public static class NotificationStatus
{
    public const string Pending = "Pending";
    public const string Sent = "Sent";
    /// <summary>Nothing to deliver to: the recipient has no registered device (the notification stays visible in the inbox).</summary>
    public const string Skipped = "Skipped";
    public const string Failed = "Failed";
}
