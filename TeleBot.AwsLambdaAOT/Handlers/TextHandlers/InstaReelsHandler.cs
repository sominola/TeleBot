using System.Web;
using Microsoft.Extensions.Logging;
using MimeTypes;
using TeleBot.Lib;
using TeleBot.Lib.Models;

namespace TeleBot.AwsLambdaAOT.Handlers.TextHandlers;

public class InstaReelsHandler(
    IHttpClientFactory httpClientFactory,
    ILogger<InstaReelsHandler> logger
) : IMessageHandler
{
    private readonly HttpClient _defaultHttpClient = httpClientFactory.CreateClient("Default");
    private const string UserAgent = "Telegram/31192 CFNetwork/1492.0.1 Darwin/23.3.0";

    public async Task Handle(ITeleBot botClient, Message message, CancellationToken ct = default)
    {
        logger.LogInformation("Processing Insta message");

        using var instaHttpMessage = BuildHttpMessage(message.Text!);
        using var contentResponse = await _defaultHttpClient.SendAsync(instaHttpMessage, ct);
        logger.LogInformation("InstaFile downloaded");

        if (!contentResponse.IsSuccessStatusCode)
        {
            var contentResponseText = await contentResponse.Content.ReadAsStringAsync(ct);
            logger.LogInformation("ContentResponse str. Response {ResponseText} {HttpCode}",
                contentResponseText,
                contentResponse.StatusCode
            );
            return;
        }

        logger.LogInformation("ContentResponse {HttpCode}", contentResponse.StatusCode);

        var contentType = contentResponse.Content.Headers.ContentType;
        var fileExtension = MimeTypeMap.GetExtension(contentType!.MediaType);

        var isVideo = fileExtension is
            ".mp4" or ".webm" or ".mov" or ".mkv" or ".avi" or ".3gp" or ".m4v";
        var isPhoto = fileExtension is
            ".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp" or ".gif" or ".tiff" or ".heic";

        if (!isVideo && !isPhoto)
        {
            logger.LogWarning("Unprocessable content type. ContentType: {ContentType}", contentType);
            return;
        }

        await using var stream = await contentResponse.Content.ReadAsStreamAsync(ct);

        if (isVideo)
        {
            await botClient.SendVideo(
                message.Chat.Id,
                stream,
                $"{Guid.NewGuid()}{fileExtension}",
                hasSpoiler: false,
                disableNotification: true,
                replyToMessageId: message.MessageId,
                ct: ct);
        }
        else if (isPhoto)
        {
            await botClient.SendPhoto(
                message.Chat.Id,
                stream,
                $"{Guid.NewGuid()}{fileExtension}",
                hasSpoiler: false,
                disableNotification: true,
                replyToMessageId: message.MessageId,
                ct: ct
            );
        }
    }

    private static HttpRequestMessage BuildHttpMessage(string contentUrl)
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

        var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);
        request.Headers.UserAgent.ParseAdd(UserAgent);

        return request;
    }
}
