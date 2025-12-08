using TeleBot.Lib.Models;

namespace TeleBot.Lib;

public interface ITeleBot
{
    Task<TeleResult> GetMe(CancellationToken ct = default);

    Task<TeleResult> SendPhoto(
        long chatId,
        Stream stream,
        string fileName,
        bool? hasSpoiler = default,
        bool? disableNotification = default,
        int? replyToMessageId = default,
        CancellationToken ct = default);

    Task<TeleResult> SendVideo(
        long chatId,
        Stream videoUrl,
        string fileName,
        bool? hasSpoiler = default,
        bool? disableNotification = default,
        int? replyToMessageId = default,
        CancellationToken ct = default);

    Task<TeleResult> SendMessage(
        long chatId,
        string text,
        bool? disableNotification = default,
        int? replyToMessageId = default,
        CancellationToken ct = default);
}

public class TeleBot : ITeleBot
{
    private const string BaseAddress = "https://api.telegram.org/bot";
    private readonly TeleClient _teleClient;

    public TeleBot(string token)
    {
        _teleClient = new TeleClient(BaseAddress, token);
    }

    public async Task<TeleResult> GetMe(CancellationToken ct = default) =>
        await _teleClient.Get<TeleResult>("getMe", ct);

    public async Task<TeleResult> SendPhoto(
        long chatId,
        Stream stream,
        string fileName,
        bool? hasSpoiler = default,
        bool? disableNotification = default,
        int? replyToMessageId = default,
        CancellationToken ct = default)
    {
        var keys = new Dictionary<string, string>
        {
            { "chat_id", chatId.ToString() },
            { "has_spoiler", hasSpoiler.GetValueOrDefault().ToString() },
            { "disable_notification", disableNotification.GetValueOrDefault().ToString() },
            { "reply_to_message_id", replyToMessageId.GetValueOrDefault().ToString() }
        };

        var file = new ValueTuple<Stream, string, string>(stream, fileName, "photo");

        return await _teleClient.PostMultipartContent<TeleResult>("sendPhoto", keys, file, ct);
    }


    public async Task<TeleResult> SendVideo(
        long chatId,
        Stream videoUrl,
        string fileName,
        bool? hasSpoiler = default,
        bool? disableNotification = default,
        int? replyToMessageId = default,
        CancellationToken ct = default)
    {
        var keys = new Dictionary<string, string>
        {
            { "chat_id", chatId.ToString() },
            { "has_spoiler", hasSpoiler.GetValueOrDefault().ToString() },
            { "disable_notification", disableNotification.GetValueOrDefault().ToString() },
            { "reply_to_message_id", replyToMessageId.GetValueOrDefault().ToString() }
        };

        var file = new ValueTuple<Stream, string, string>(videoUrl, fileName, "video");

        return await _teleClient.PostMultipartContent<TeleResult>("sendVideo", keys, file, ct);
    }

    public async Task<TeleResult> SendMessage(
        long chatId,
        string text,
        bool? disableNotification = default,
        int? replyToMessageId = default,
        CancellationToken ct = default
    )
    {
        var keys = new Dictionary<string, string>
        {
            { "chat_id", chatId.ToString() },
            { "text", text },
            { "disable_notification", disableNotification.GetValueOrDefault().ToString() }
        };

        if (replyToMessageId.HasValue)
            keys.Add("reply_to_message_id", replyToMessageId.Value.ToString());

        return await _teleClient.Post<Dictionary<string, string>, TeleResult>("sendMessage", keys, ct);
    }
}
