using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeTypes;
using TeleBot.AwsLambdaAOT.Options;
using TeleBot.AwsLambdaAOT.Responses;
using TeleBot.Lib;
using TeleBot.Lib.Models;

namespace TeleBot.AwsLambdaAOT.Handlers.TextHandlers;

public class InstaReelsHandler(
    IHttpClientFactory httpClientFactory,
    ILogger logger,
    IOptions<AppOptions> options
) : IMessageHandler
{
    private readonly HttpClient _defaultHttpClient = httpClientFactory.CreateClient("Default");

    public async Task Handle(ITeleBot botClient, Message message, CancellationToken ct = default)
    {
        logger.LogInformation("Processing Insta message");
        using var requestMessage = BuildMessage(message.Text!);

        using var response = await _defaultHttpClient.SendAsync(requestMessage, ct);
        if (response.IsSuccessStatusCode)
        {
            var instaResponse = await response.Content
                .ReadFromJsonAsync(LambdaJsonContext.Default.FastDlResponse, ct);

            if (instaResponse is null)
                throw new Exception("InstaResponse is null, return");

            var (url, isVideo) = GetUrl(instaResponse);
            logger.LogInformation("URL: {Url}", url);

            using var contentResponse = await _defaultHttpClient.GetAsync(url, ct);
            var extension = MimeTypeMap.GetExtension(contentResponse.Content.Headers.ContentType!.MediaType);
            await using var stream = await contentResponse.Content.ReadAsStreamAsync(ct);

            if (isVideo)
            {
                await botClient.SendVideo(
                    message.Chat.Id,
                    stream,
                    $"{Guid.NewGuid()}{extension}",
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
                    $"{Guid.NewGuid()}{extension}",
                    hasSpoiler: false,
                    disableNotification: true,
                    replyToMessageId: message.MessageId,
                    ct: ct
                );
            }
        }
    }

    private static (string url, bool isVideo) GetUrl(FastDlResponse response)
    {
        var isVideo = response.Url.Any(x => x.Type == "mp4");
        if (isVideo)
        {
            var videoObj = response.Url.FirstOrDefault(x => x is { Type: "mp4", Quality: 1080 } || x.Type == "mp4");
            return (videoObj!.Url, true);
        }

        var photoObj = response.Url.FirstOrDefault(x => x.Type is "heic" or "jpg");
        if (photoObj is null)
            throw new Exception($"{nameof(photoObj)} is null");

        return (photoObj.Url, false);
    }

    private HttpRequestMessage BuildMessage(string url)
    {
        var body = new FastDlRequest
        {
            Url = url,
            Ts = 1719053861511,
            _Ts = 1718869499625,
            _Tsc = 784590,
            _S = "e75cf6ed60570c6bd189433a513e5a9c770c169cd48c52ac6df800a138857fea"
        };

        var serialized = JsonSerializer.Serialize(body, LambdaJsonContext.Default.FastDlRequest);
        var req = new HttpRequestMessage(HttpMethod.Post, options.Value.InstagramApiUrl)
        {
            Content = new StringContent(serialized, Encoding.UTF8, MediaTypeNames.Application.Json)
        };

        req.Headers.Add("Accept", "*/*");
        req.Headers.Add("Accept-Language", "en-US,en;q=0.5");
        req.Headers.Add("Sec-Fetch-Dest", "empty");
        req.Headers.Add("Sec-Fetch-Mode", "cors");
        req.Headers.Add("Sec-Fetch-Site", "same-origin");
        req.Headers.Add("User-Agent",
            "Mozilla/5.0 (Linux; Android 11; SAMSUNG SM-G973U) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/14.2 Chrome/87.0.4280.141 Mobile Safari/537.36");

        return req;
    }
}
