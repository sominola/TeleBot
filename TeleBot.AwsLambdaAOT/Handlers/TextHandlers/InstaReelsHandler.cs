using System.Web;
using Microsoft.Extensions.Logging;
using TeleBot.Lib;
using TeleBot.Lib.Models;

namespace TeleBot.AwsLambdaAOT.Handlers.TextHandlers;

public class InstaReelsHandler(
    MediaStreamService mediaStreamService,
    ILogger<InstaReelsHandler> logger
) : IMessageHandler
{
    public async Task Handle(ITeleBot botClient, Message message, CancellationToken ct = default)
    {
        logger.LogInformation("Processing Insta message");

        var mediaUrl = BuildMediaUrl(message.Text!);
        await using var media = await mediaStreamService.Get(mediaUrl, ct);

        if (media.Type == MediaType.Video)
        {
            await botClient.SendVideo(
                message.Chat.Id,
                media.Stream,
                $"{Guid.NewGuid()}{media.Extension}",
                disableNotification: true,
                replyToMessageId: message.MessageId,
                ct: ct);
        }
        else
        {
            await botClient.SendPhoto(
                message.Chat.Id,
                media.Stream,
                $"{Guid.NewGuid()}{media.Extension}",
                disableNotification: true,
                replyToMessageId: message.MessageId,
                ct: ct);
        }
    }

    private static string BuildMediaUrl(string contentUrl)
    {
        if (!Uri.TryCreate(contentUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(uri.Host, "www.instagram.com", StringComparison.OrdinalIgnoreCase) ||
            !string.IsNullOrEmpty(uri.UserInfo))
        {
            throw new ArgumentException("Invalid Instagram URL.", nameof(contentUrl));
        }

        var query = HttpUtility.ParseQueryString(uri.Query);
        query.Remove("igsh");

        var uriBuilder = new UriBuilder(uri)
        {
            Host = "www.kkinstagram.com",
            Port = -1,
            Query = query.ToString(),
        };

        return uriBuilder.Uri.ToString();
    }
}
