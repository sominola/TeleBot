using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Microsoft.Extensions.Logging;
using MimeTypes;
using TeleBot.Lib;
using TeleBot.Lib.Models;

namespace TeleBot.AwsLambdaAOT.Handlers.TextHandlers;

public class InstaReelsHandler(
    IHttpClientFactory httpClientFactory,
    ILogger logger
) : IMessageHandler
{
    private readonly HttpClient _defaultHttpClient = httpClientFactory.CreateClient("Default");
    private const string UserAgent = "Telegram/31192 CFNetwork/1492.0.1 Darwin/23.3.0";

    public async Task Handle(ITeleBot botClient, Message message, CancellationToken ct = default)
    {
        logger.LogInformation("Processing Insta message");

        var url = RemoveIgsh(message.Text!);
        using var instaHttpMessage = BuildHttpMessage(url);
        using var contentResponse = await _defaultHttpClient.SendAsync(instaHttpMessage, ct);
        logger.LogInformation("InstaFile downloaded");

        if (!contentResponse.IsSuccessStatusCode)
        {
            var contentResponseText = await contentResponse.Content.ReadAsStringAsync(ct);
            logger.LogInformation("ContentResponse str. Response {ResponseText} {HttpCode}",
                contentResponseText,
                contentResponse.StatusCode
            );
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

    private HttpRequestMessage BuildHttpMessage(string contentUrl)
    {
        var url = contentUrl.Replace("https://www.instagram.com", "https://www.kkinstagram.com");
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.UserAgent.ParseAdd(UserAgent);

        return request;
    }

    private static string RemoveIgsh(string contentUrl)
    {
        var uri = new Uri(contentUrl);
        var query = HttpUtility.ParseQueryString(uri.Query);
        query.Remove("igsh");
        var uriBuilder = new UriBuilder(uri)
        {
            Query = query.ToString(),
        };

        return uriBuilder.ToString();
    }
}
