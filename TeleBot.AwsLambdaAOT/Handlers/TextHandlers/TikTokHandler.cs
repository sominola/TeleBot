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
    private readonly HttpClient _tikWmClient = clientFactory.CreateClient("Tikwm");
    private readonly HttpClient _prostoyClient = clientFactory.CreateClient("Prostoy");

    public async Task Handle(ITeleBot botClient, Message message, CancellationToken ct = default)
    {
        logger.LogInformation("Processing TikTok message");
        var query = $"?url={message.Text!}?hd=1";

        using var response = await _tikWmClient.GetAsync(query, ct);
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content
                .ReadFromJsonAsync(LambdaJsonContext.Default.TikWmResponse, ct);
            if (result?.Data is null)
                return;

            if (result.Data.Duration.HasValue && result.Data.Duration > 0)
            {
                var videoUrl = result.Data.Play;
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
            else if (result.Data.Images.Length != 0)
            {
                // var photoMedias = result.Data.Images
                //     .Select(x => new InputMediaPhoto(new InputFileUrl(x)))
                //     .ToList<IAlbumInputMedia>();
                //
                //
                // await botClient.SendMediaGroupAsync(
                //     message.Chat.Id,
                //     photoMedias,
                //     cancellationToken: ct
                // );
                //
                // await botClient.SendAudioAsync(
                //     message.Chat.Id,
                //     new InputFileUrl(result.Data.Music),
                //     title: result.Data.Title,
                //     cancellationToken: ct
                // );
            }
        }
        else
        {
            // var error = await response.Content.ReadAsStringAsync(ct);
            // await botClient.SendTextMessageAsync(
            //     message.Chat.Id,
            //     $"Error: \n{error}",
            //     replyToMessageId: message.MessageId,
            //     cancellationToken: ct);
        }
    }
}
