namespace Genkan.Notification.Domain.Entities;

/// <summary>
/// Membership of a user in a named device group. FCM's own device groups are deprecated (shut down 2027-09-29),
/// so the group -> members mapping is kept here and a send resolves it to registration tokens (multicast).
/// </summary>
public class DeviceGroupMember
{
    public Guid Id { get; set; }
    public string GroupName { get; set; } = "";
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
