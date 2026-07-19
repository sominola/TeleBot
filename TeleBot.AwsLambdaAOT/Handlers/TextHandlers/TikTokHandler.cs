using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using TeleBot.Lib;
using TeleBot.Lib.Models;

namespace TeleBot.AwsLambdaAOT.Handlers.TextHandlers;

public class TikTokHandler(
    IHttpClientFactory clientFactory,
    MediaStreamService mediaStreamService,
    ILogger<TikTokHandler> logger
) : IMessageHandler
{
    private readonly HttpClient _defaultHttpClient = clientFactory.CreateClient("Default");

    public async Task Handle(ITeleBot botClient, Message message, CancellationToken ct = default)
    {
        logger.LogInformation("Processing TikTok message");
        const string baseUrl = "https://www.tikwm.com/api/";
        var query = $"{baseUrl}?url={Uri.EscapeDataString(message.Text!)}&hd=1";

        using var response = await _defaultHttpClient.GetAsync(query, ct);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content
                .ReadFromJsonAsync(LambdaJsonContext.Default.TikWmResponse, ct);
            if (result?.Data is null)
                return;

            if (result.Data.Duration.HasValue && result.Data.Duration > 0)
            {
                await using var media = await mediaStreamService.Get(result.Data.Play, ct);

                if (media.Type == MediaType.Video)
                {
                    await botClient.SendVideo(
                        message.Chat.Id,
                        media.Stream,
                        $"{Guid.NewGuid()}{media.Extension}",
                        disableNotification: true,
                        replyToMessageId: message.MessageId,
                        ct: ct);
                }
                else
                {
                    await botClient.SendPhoto(
                        message.Chat.Id,
                        media.Stream,
                        $"{Guid.NewGuid()}{media.Extension}",
                        disableNotification: true,
                        replyToMessageId: message.MessageId,
                        ct: ct);
                }
            }
        }
        else
        {
            var responseStr = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Error while process TikTok response: {ResponseStr}", responseStr);
        }
    }

}
