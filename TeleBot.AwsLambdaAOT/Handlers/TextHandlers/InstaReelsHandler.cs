using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MimeTypes;
using TeleBot.AwsLambdaAOT.Extensions;
using TeleBot.Lib;
using TeleBot.Lib.Models;

namespace TeleBot.AwsLambdaAOT.Handlers.TextHandlers;

public partial class InstaReelsHandler(
    IHttpClientFactory httpClientFactory,
    ILogger logger
) : IMessageHandler
{
    private readonly HttpClient _defaultHttpClient = httpClientFactory.CreateClient("Default");

    public async Task Handle(ITeleBot botClient, Message message, CancellationToken ct = default)
    {
        logger.LogInformation("Processing Insta message");

        var reelsId = ExtractReelId(message.Text!);
        if (string.IsNullOrEmpty(reelsId))
        {
            logger.LogWarning("ReelsId is null or empty");
            return;
        }

        logger.LogInformation("ReelsId: {reelsId}", reelsId);

        using var mediaIdRequest = GetMediaIdRequest(reelsId);
        using var mediaIdResponse = await _defaultHttpClient.SendAsync(mediaIdRequest, ct);
        if (!mediaIdResponse.IsSuccessStatusCode)
        {
            var responseText = await mediaIdResponse.Content.ReadAsStringAsync(ct);
            logger.LogError("MediaIdResponse was not success. Response {ResponseText}", responseText);
            return;
        }

        var instaMediaIdObj = await mediaIdResponse.Content
            .ReadFromJsonAsync(LambdaJsonContext.Default.InstagramMediaResponse, ct);

        var contentUrl = instaMediaIdObj?.Data.InstagramXdt.VideoUrl;

        if (string.IsNullOrEmpty(contentUrl))
        {
            logger.LogWarning("ContentUrl is null or empty");
            return;
        }

        logger.LogInformation("ContentUrl is {ContentUrl}", contentUrl);

        using var contentResponse = await _defaultHttpClient.GetAsync(contentUrl, ct);
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

        var isVideo = fileExtension!.Contains("mp4");
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
        else
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

    private static HttpRequestMessage GetMediaIdRequest(string reelsId)
    {
        var nameValues = new List<KeyValuePair<string, string>>(HttpHeaders.Insta.UrlContent)
        {
            HttpHeaders.Insta.GetContentPostId(reelsId)
        };
        var encodedData = new FormUrlEncodedContent(nameValues);

        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://www.instagram.com/api/graphql")
        {
            Content = encodedData
        };
        foreach (var (key, value) in HttpHeaders.Insta.Headers)
            requestMessage.Headers.Add(key, value);

        return requestMessage;
    }

    private static string? ExtractReelId(string url)
    {
        var regex = MyRegex();
        var match = regex.Match(url);

        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"(?:https?:\/\/(?:www\.)?instagram\.com\/(?:reels?|p)\/)([\w-]+)")]
    private static partial Regex MyRegex();
}
