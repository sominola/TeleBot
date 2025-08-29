using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MimeTypes;
using TeleBot.AwsLambdaAOT.Responses;
using TeleBot.Lib;
using TeleBot.Lib.Models;

namespace TeleBot.AwsLambdaAOT.Handlers.TextHandlers;

public class InstaReelsHandler(
    IHttpClientFactory httpClientFactory,
    ILogger logger
) : IMessageHandler
{
    private readonly HttpClient _defaultHttpClient = httpClientFactory.CreateClient("Default");
    private const string IgramHostname = "api.igram.world";
    private const string IgramKey = "aaeaf2805cea6abef3f9d2b6a666fce62fd9d612a43ab772bb50ce81455112e0";
    private const string IgramTimestamp = "1742201548873";

    private const string UserAgent = "Mozilla/5.0 (Linux; Android 10; SM-G960U) AppleWebKit/537.36 " +
                                     "(KHTML, like Gecko) Chrome/88.0.4324.181 Mobile Safari/537.36";

    public async Task Handle(ITeleBot botClient, Message message, CancellationToken ct = default)
    {
        logger.LogInformation("Processing Insta message");

        var payload = BuildGramPayload(message.Text!);
        using var gramHttpMessage = BuildGramHttpMessage(payload);
        using var gramResponse = await _defaultHttpClient.SendAsync(gramHttpMessage, ct);
        if (!gramResponse.IsSuccessStatusCode)
        {
            var responseText = await gramResponse.Content.ReadAsStringAsync(ct);
            logger.LogError("GramResponse was not success. Response {ResponseText}", responseText);
            return;
        }

        var gramObj = await gramResponse.Content
            .ReadFromJsonAsync(LambdaJsonContext.Default.GramResponse, ct);

        var contentUrl = gramObj?.Urls.FirstOrDefault()?.Url;

        if (string.IsNullOrEmpty(contentUrl))
        {
            logger.LogWarning("ContentUrl is null or empty");
            return;
        }

        logger.LogInformation("ContentUrl is {ContentUrl}", contentUrl);

        using var downloadMessage = BuildDownloadMessage(contentUrl);
        using var contentResponse = await _defaultHttpClient.SendAsync(downloadMessage, ct);
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

    private static HttpRequestMessage BuildDownloadMessage(string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        request.Headers.UserAgent.ParseAdd(UserAgent);

        return request;
    }

    private HttpRequestMessage BuildGramHttpMessage(GramPayload payload)
    {
        var apiUrl = $"https://{IgramHostname}/api/convert";
        var payloadJson = JsonSerializer.Serialize(payload, LambdaJsonContext.Default.GramPayload);
        var request = new HttpRequestMessage(HttpMethod.Post, apiUrl);
        request.Content = new StringContent(payloadJson, Encoding.UTF8, MediaTypeNames.Application.Json);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
        request.Headers.UserAgent.ParseAdd(UserAgent);

        return request;
    }

    private static GramPayload BuildGramPayload(string contentUrl)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var hashInput = contentUrl + timestamp + IgramKey;
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(hashInput));
        var secret = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();

        var payloadObj = new GramPayload
        {
            Url = contentUrl,
            Timestamp = timestamp,
            GramTimestamp = IgramTimestamp,
            Tsc = "0",
            Signature = secret
        };

        return payloadObj;
    }
}
