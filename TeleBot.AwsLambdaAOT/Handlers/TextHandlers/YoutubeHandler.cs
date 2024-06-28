using System.Net.Http.Headers;
using System.Net.Http.Json;
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

public class YoutubeHandler(ILogger logger, IHttpClientFactory factory, IOptions<AppOptions> options) : IMessageHandler
{
    private readonly HttpClient _defaultHttpClient = factory.CreateClient("Default");

    public async Task Handle(ITeleBot botClient, Message message, CancellationToken ct = default)
    {
        logger.LogInformation("Processing youtube message");
        using var requestMessage = BuildMessage(message.Text!);

        using var response = await _defaultHttpClient.SendAsync(requestMessage, ct);
        if (response.IsSuccessStatusCode)
        {
            var youtubeUrlResponse = await response.Content
                .ReadFromJsonAsync(LambdaJsonContext.Default.YoutubeUrlResponse, ct);

            var videoUrl = youtubeUrlResponse!.Url;
            using var videoResponse = await _defaultHttpClient.GetAsync(videoUrl, ct);
            var videoExtension = MimeTypeMap.GetExtension(videoResponse.Content.Headers.ContentType!.MediaType);
            await using var videoStream = await videoResponse.Content.ReadAsStreamAsync(ct);

            await botClient.SendVideo(
                message.Chat.Id,
                videoStream,
                $"{Guid.NewGuid()}{videoExtension}",
                hasSpoiler: false,
                disableNotification: true,
                replyToMessageId: message.MessageId,
                ct: ct);
        }
    }

    private HttpRequestMessage BuildMessage(string url)
    {
        var json = JsonSerializer.Serialize(new YoutubeUrlRequest
        {
            Url = url
        }, LambdaJsonContext.Default.YoutubeUrlRequest);

        var req = new HttpRequestMessage(HttpMethod.Post, options.Value.YoutubeApiUrl)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        req.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        req.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        req.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        req.Headers.AcceptEncoding.Add(new StringWithQualityHeaderValue("zstd"));
        req.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("ru-RU"));
        req.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("ru", 0.9));
        req.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en-US", 0.8));
        req.Headers.AcceptLanguage.Add(new StringWithQualityHeaderValue("en", 0.7));
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        req.Headers.Add("Origin", "https://cobalt.tools");
        req.Headers.Add("Referer", "https://cobalt.tools/");
        req.Headers.Add("Sec-Ch-Ua", "\"Chromium\";v=\"124\", \"Google Chrome\";v=\"124\", \"Not-A.Brand\";v=\"99\"");
        req.Headers.Add("Sec-Ch-Ua-Mobile", "?0");
        req.Headers.Add("Sec-Ch-Ua-Platform", "\"Windows\"");
        req.Headers.Add("Sec-Fetch-Dest", "empty");
        req.Headers.Add("Sec-Fetch-Mode", "cors");
        req.Headers.Add("Sec-Fetch-Site", "cross-site");
        req.Headers.UserAgent.ParseAdd(
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36");

        return req;
    }
}
