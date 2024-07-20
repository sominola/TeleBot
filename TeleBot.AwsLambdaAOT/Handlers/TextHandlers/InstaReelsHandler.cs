using System.Net.Http.Json;
using System.Net.Mime;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeTypes;
using TeleBot.AwsLambdaAOT.Options;
using TeleBot.AwsLambdaAOT.Responses;
using TeleBot.Lib;
using TeleBot.Lib.Models;

namespace TeleBot.AwsLambdaAOT.Handlers.TextHandlers;

public partial class InstaReelsHandler(
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
                .ReadFromJsonAsync(LambdaJsonContext.Default.InstsaResponse, ct);

            if (instaResponse is null)
                throw new Exception("InstaResponse is null, return");

            var url = GetUrl(instaResponse);
            logger.LogInformation("URL: {Url}", url);

            using var contentResponse = await _defaultHttpClient.GetAsync(url, ct);
            var extension = MimeTypeMap.GetExtension(contentResponse.Content.Headers.ContentType!.MediaType);
            var isVideo = extension.Contains("mp4");
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

    private static string GetUrl(InstsaResponse response)
    {
        var match = MyRegex().Match(response.Data);

        if (!match.Success) throw new Exception("Cannot get insta url");
        var url = match.Groups[1].Value.Replace("amp;", "");
        return url;

    }

    private HttpRequestMessage BuildMessage(string url)
    {
        var dic = new Dictionary<string, string>()
        {
            { "q", url },
            { "t", "media" },
            { "lang", "ru" },
        };

        var req = new HttpRequestMessage(HttpMethod.Post, options.Value.InstagramApiUrl)
        {
            Content = new FormUrlEncodedContent(dic)
        };

        req.Headers.Add("Accept", "application/json, text/plain, */*");
        req.Headers.Add("Accept-Encoding", "gzip, deflate, br, zstd");
        req.Headers.Add("Accept-Language", "uk-UA,uk;q=0.9,ru-UA;q=0.8,ru;q=0.7,en-US;q=0.6,en;q=0.5");
        req.Headers.Add("Cookie", "uid=a8d272e5c2c41435; adsAfterSearch=67; adsPopupClick=79; adsForm=30");
        req.Headers.Add("Origin", "https://saveig.app");
        req.Headers.Add("Referer", "https://saveig.app/");
        req.Headers.Add("Priority", "nu=1, i");
        req.Headers.Add("Sec-Ch-Ua", "\"Not/A)Brand\";v=\"8\", \"Chromium\";v=\"126\", \"Google Chrome\";v=\"126\"");
        req.Headers.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
        req.Headers.Add("Sec-Fetch-Dest", "empty");
        req.Headers.Add("Sec-Fetch-Mode", "cors");
        req.Headers.Add("Sec-Fetch-Site", "same-origin");
        req.Headers.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/126.0.0.0 Safari/537.36");


        return req;
    }

    [GeneratedRegex("href=\"(https://[^\"]+)\"")]
    private static partial Regex MyRegex();
}
