using FirebaseAdmin.Messaging;
using Genkan.Notification.Domain.Dto;
using Genkan.Notification.Infrastructure.Interfaces;

namespace Genkan.Notification.Infrastructure.Push;

/// <summary>Individual notification: one message to one registration token.</summary>
public sealed class FcmIndividualPushSender(FirebaseMessaging fcm) : IIndividualPushSender
{
    public async Task<PushResult> SendAsync(string token, PushMessage message, CancellationToken ct)
    {
        try
        {
            var id = await fcm.SendAsync(FcmMessageBuilder.ForToken(token, message), ct);
            return new PushResult(1, 0, [], null, id);
        }
        catch (FirebaseMessagingException ex) when (ex.MessagingErrorCode == MessagingErrorCode.Unregistered)
        {
            return new PushResult(0, 1, [token]);
        }
        catch (FirebaseMessagingException ex)
        {
            return new PushResult(0, 1, [], ex.Message);
        }
    }
}
