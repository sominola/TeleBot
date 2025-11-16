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
    private const string ApiHostName = "api-wh.igram.world";
    private const string ApiFullUrl = $"https://{ApiHostName}/api/convert";
    private const string HexKey = "36fc819c862897305f027cda96822a071a4a01b7f46bb4ffaac9b88a649d9c28";
    private const string Timestamp = "1763129421273";

    private const string UserAgent = "Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) " +
                                     "AppleWebKit/605.1.15 (KHTML, like Gecko) " +
                                     "Version/17.0 Mobile/15E148 Safari/604.1";

    public async Task Handle(ITeleBot botClient, Message message, CancellationToken ct = default)
    {
        logger.LogInformation("Processing Insta message");

        var url = RemoveIgsh(message.Text!);
        using var gramHttpMessage = BuildGramHttpMessage(url);
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

    private HttpRequestMessage BuildGramHttpMessage(string contentUrl)
    {
        var strTs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(contentUrl + strTs + HexKey));
        var hexSecret = Convert.ToHexString(bytes).ToLower();

        var dict = new Dictionary<string, string>()
        {
            ["sf_url"] = contentUrl,
            ["ts"] = strTs,
            ["_ts"] = Timestamp,
            ["_tsc"] = "0",
            ["_s"] = hexSecret,
        };

        var request = new HttpRequestMessage(HttpMethod.Post, ApiFullUrl)
        {
            Content = new FormUrlEncodedContent(dict),
        };

        request.Headers.Accept.ParseAdd("*/*");
        request.Headers.UserAgent.ParseAdd(UserAgent);
        request.Headers.Referrer = new Uri("https://igram.world/");
        request.Headers.Add("origin", "https://igram.world");

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
