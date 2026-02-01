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
    private const string ApiHostName = "api-wh.fastdl.app";
    private const string ApiFullUrl = $"https://{ApiHostName}/api/convert";
    private const string HexKey = "970514c817fe374ff071f0ef8ba229fcc3fae9541126c5763161eac4668b7a55";
    private const string Timestamp = "1769592795476";

    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) " +
                                     "AppleWebKit/537.36 (KHTML, like Gecko) " +
                                     "Chrome/144.0.0.0 Safari/537.36";

    public async Task Handle(ITeleBot botClient, Message message, CancellationToken ct = default)
    {
        logger.LogInformation("Processing Insta message");

        var url = RemoveIgsh(message.Text!);
        using var gramHttpMessage = BuildGramHttpMessage(url);
        using var gramResponse = await _defaultHttpClient.SendAsync(gramHttpMessage, ct);
        
        logger.LogInformation("Version response: {HttpVersion}", gramResponse.Version);
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
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionExact,
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
