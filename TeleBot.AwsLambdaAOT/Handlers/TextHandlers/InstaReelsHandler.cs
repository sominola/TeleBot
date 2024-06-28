using System.Net.Http.Json;
using System.Net.Mime;
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
    private readonly HttpClient _prostoyClient = httpClientFactory.CreateClient("Prostoy");

    public async Task Handle(ITeleBot botClient, Message message, CancellationToken ct = default)
    {
        logger.LogInformation("Processing Insta message");
        using var requestMessage = BuildMessage(message.Text!);

        using var response = await _prostoyClient.SendAsync(requestMessage, ct);
        if (response.IsSuccessStatusCode)
        {
            var instaResponse = await response.Content
                .ReadFromJsonAsync(LambdaJsonContext.Default.FastDlResponse, ct);

            var videoUrl = instaResponse!.Url.First().Url;
            logger.LogInformation("Video URL: {Url}", videoUrl);

            using var videoResponse = await _prostoyClient.GetAsync(videoUrl, ct);
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
 
    private static HttpRequestMessage BuildMessage(string url)
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

        var req = new HttpRequestMessage(HttpMethod.Post, "https://fastdl.app/api/convert")
            { Content = new StringContent(serialized, Encoding.UTF8, MediaTypeNames.Application.Json) };
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
