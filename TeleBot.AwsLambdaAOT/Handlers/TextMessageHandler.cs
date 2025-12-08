using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TeleBot.AwsLambdaAOT.Handlers.TextHandlers;
using TeleBot.AwsLambdaAOT.Options;
using TeleBot.Lib;
using TeleBot.Lib.Models;
using TeleBot.Lib.Models.Enums;

namespace TeleBot.AwsLambdaAOT.Handlers;

public record PatternAndHandler(HashSet<string> Patterns, IMessageHandler Handler);

public class TextMessageHandler(
    InstaReelsHandler instaReelsHandler,
    TikTokHandler tikTokHandler,
    DeepSeekHandler deepSeekHandler,
    ILogger logger,
    IOptions<AppOptions> options
)
{
    public async Task Handle(ITeleBot botClient, Message message, CancellationToken ct = default)
    {
        if (await CheckAndProcessDeepSeekMessage(botClient, message, ct)) return;

        var handlers = new List<PatternAndHandler>
        {
            new(["https://vm.tiktok.com", "https://www.tiktok.com", "https://m.tiktok.com", "https://vt.tiktok.com"],
                tikTokHandler),
            new(["https://www.instagram.com",], instaReelsHandler),
        };

        var handler = handlers.Where(x => x.Patterns
                .Any(y => message.Text!.StartsWith(y, StringComparison.OrdinalIgnoreCase)))
            .Select(x => x.Handler)
            .FirstOrDefault();

        if (handler is not null)
            await handler.Handle(botClient, message, ct);
        else
            logger.LogInformation("No handlers for {Type}, return", nameof(MessageType.Text));
    }

    private async Task<bool> CheckAndProcessDeepSeekMessage(ITeleBot botClient, Message message, CancellationToken ct)
    {
        var mentioned = message.Entities?
            .Where(x => x.Type == "mention")
            .FirstOrDefault();

        var text = message.Text;

        if (mentioned is not null && !string.IsNullOrWhiteSpace(text))
        {
            var userName = text.Substring(mentioned.Offset, mentioned.Length);
            if (userName.Equals(options.Value.TelegramBotName, StringComparison.OrdinalIgnoreCase))
            {
                logger.LogInformation("Handling DeepSeek request for mention {UserName}", userName);
                await deepSeekHandler.Handle(botClient, message, ct);
                return true;
            }
        }

        return false;
    }
}
