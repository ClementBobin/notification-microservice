namespace Genkan.Notification.Domain.Configuration;

/// <summary>
/// Public client-side identifiers of the Firebase project (what google-services.json contains), served to the mobile app
/// so it can initialise Firebase at runtime. These are NOT secrets; the service-account key never goes in here.
/// </summary>
public sealed class FirebaseClientOptions
{
    public string ApiKey { get; set; } = "";
    /// <summary>Android: mobilesdk_app_id, for example 1:123456789012:android:abc123</summary>
    public string ApplicationId { get; set; } = "";
    /// <summary>The project number.</summary>
    public string MessagingSenderId { get; set; } = "";
    public string ProjectId { get; set; } = "";
}
