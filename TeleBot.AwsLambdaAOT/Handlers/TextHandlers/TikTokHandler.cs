using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using MimeTypes;
using TeleBot.Lib;
using TeleBot.Lib.Models;

namespace TeleBot.AwsLambdaAOT.Handlers.TextHandlers;

public class TikTokHandler(
    IHttpClientFactory clientFactory,
    ILogger logger
) : IMessageHandler
{
    private readonly HttpClient _defaultHttpClient = clientFactory.CreateClient("Default");

    public async Task Handle(ITeleBot botClient, Message message, CancellationToken ct = default)
    {
        logger.LogInformation("Processing TikTok message");
        const string baseUrl = "https://www.tikwm.com/api/";
        var query = baseUrl + $"?url={message.Text!}?hd=1";

        using var response = await _defaultHttpClient.GetAsync(query, ct);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content
                .ReadFromJsonAsync(LambdaJsonContext.Default.TikWmResponse, ct);
            if (result?.Data is null)
                return;

            if (result.Data.Duration.HasValue && result.Data.Duration > 0)
            {
                var videoUrl = result.Data.Play;
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
        else
        {
            var responseStr = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Error while process TikTok response: {ResponseStr}", responseStr);
            return;
        }
    }
}
