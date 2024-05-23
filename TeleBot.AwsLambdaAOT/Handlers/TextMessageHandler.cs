using Microsoft.Extensions.Logging;
using TeleBot.AwsLambdaAOT.Handlers.TextHandlers;
using TeleBot.Lib;
using TeleBot.Lib.Models;
using TeleBot.Lib.Models.Enums;

namespace TeleBot.AwsLambdaAOT.Handlers;

public record PatternAndHandler(HashSet<string> Patterns, IMessageHandler Handler);

public class TextMessageHandler(
    InstaReelsHandler instaReelsHandler,
    TikTokHandler tikTokHandler,
    YoutubeHandler youtubeHandler,
    ILogger logger
)
{
    public async Task Handle(ITeleBot botClient, Message message, CancellationToken ct = default)
    {
        var handlers = new List<PatternAndHandler>
        {
            new(["https://vm.tiktok.com", "https://www.tiktok.com", "https://m.tiktok.com"], tikTokHandler),
            new(["https://www.instagram.com/reel/", "https://www.instagram.com/reels/"], instaReelsHandler),
            new(["https://www.youtube.com", "https://youtu.be", "https://youtube.com/"], youtubeHandler),
        };


        var handler = handlers.Where(x => x.Patterns
                .Any(y => message.Text!.StartsWith(y, StringComparison.OrdinalIgnoreCase)))
            .Select(x => x.Handler)
            .FirstOrDefault();

        if (handler is not null)
            await handler.Handle(botClient, message, ct);
        else
            logger.LogInformation("No handlers for {Type}, return", MessageType.Text.ToString());
    }
}
