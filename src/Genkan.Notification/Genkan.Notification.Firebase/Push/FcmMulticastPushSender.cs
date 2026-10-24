using FirebaseAdmin.Messaging;
using Genkan.Notification.Domain.Dto;
using Genkan.Notification.Firebase.Interfaces;

namespace Genkan.Notification.Firebase.Push;

/// <summary>Multicast notification: one message to many registration tokens, 500 per FCM request. Also serves device groups.</summary>
public sealed class FcmMulticastPushSender(FirebaseMessaging fcm) : IMulticastPushSender
{
    private const int BatchSize = 500; // FCM multicast limit

    public async Task<PushResult> SendAsync(IReadOnlyList<string> tokens, PushMessage message, CancellationToken ct)
    {
        var accepted = 0;
        var failed = 0;
        var invalid = new List<string>();
        string? error = null;

        foreach (var batch in tokens.Chunk(BatchSize))
        {
            BatchResponse response;
            try
            {
                response = await fcm.SendEachForMulticastAsync(FcmMessageBuilder.ForTokens(batch, message), ct);
            }
            catch (FirebaseMessagingException ex)
            {
                failed += batch.Length;
                error = ex.Message;
                continue;
            }

            for (var i = 0; i < response.Responses.Count; i++)
            {
                var r = response.Responses[i];
                if (r.IsSuccess) { accepted++; continue; }

                failed++;
                if (r.Exception?.MessagingErrorCode == MessagingErrorCode.Unregistered) invalid.Add(batch[i]);
                else error = r.Exception?.Message ?? "unknown FCM error";
            }
        }

        return new PushResult(accepted, failed, invalid, error);
    }
}
