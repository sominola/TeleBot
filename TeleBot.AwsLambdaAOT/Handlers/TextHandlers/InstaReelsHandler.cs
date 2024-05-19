using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using MimeTypes;
using TeleBot.Lib;
using TeleBot.Lib.Models;

namespace TeleBot.AwsLambdaAOT.Handlers.TextHandlers;

public partial class InstaReelsHandler(
    IHttpClientFactory httpClientFactory,
    ILogger logger
) : IMessageHandler
{
    private readonly HttpClient _prostoyClient = httpClientFactory.CreateClient("Prostoy");

    public async Task Handle(ITeleBot botClient, Message message, CancellationToken ct = default)
    {
        logger.LogInformation("Processing Insta message");
        var reelCode = GetReelCodeFromUrl(message.Text!);
        
        if (string.IsNullOrEmpty(reelCode))
        {
            logger.LogWarning("Cannot get reelCode from url '{Url}", message.Text);
            return;
        }

        using var requestMessage = BuildMessage(reelCode);

        using var response = await _prostoyClient.SendAsync(requestMessage, ct);
        if (response.IsSuccessStatusCode)
        {
            var instaResponse = await response.Content
                .ReadFromJsonAsync(LambdaJsonContext.Default.InstaResponse, ct);
           
            var videoUrl = instaResponse!.Data.ShortCodeMedia.VideoUrl;
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

    private static string? GetReelCodeFromUrl(string url)
    {
        url = url.Replace("reels", "reel", StringComparison.OrdinalIgnoreCase);
        var match = MyRegex().Match(url);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }

        return null;
    }

    private static HttpRequestMessage BuildMessage(string reelCode)
    {
        var content = GetUrlContent(reelCode);

        var req = new HttpRequestMessage(HttpMethod.Post, "https://www.instagram.com/api/graphql")
            { Content = new FormUrlEncodedContent(content) };
        req.Headers.Add("Accept", "*/*");
        req.Headers.Add("Accept-Language", "en-US,en;q=0.5");
        req.Headers.Add("X-FB-Friendly-Name", "PolarisPostActionLoadPostQueryQuery");
        req.Headers.Add("X-CSRFToken", "RVDUooU5MYsBbS1CNN3CzVAuEP8oHB52");
        req.Headers.Add("X-IG-App-ID", "1217981644879628");
        req.Headers.Add("X-FB-LSD", "AVqbxe3J_YA");
        req.Headers.Add("X-ASBD-ID", "129477");
        req.Headers.Add("Sec-Fetch-Dest", "empty");
        req.Headers.Add("Sec-Fetch-Mode", "cors");
        req.Headers.Add("Sec-Fetch-Site", "same-origin");
        req.Headers.Add("User-Agent",
            "Mozilla/5.0 (Linux; Android 11; SAMSUNG SM-G973U) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/14.2 Chrome/87.0.4280.141 Mobile Safari/537.36");

        return req;
    }

    private static IEnumerable<KeyValuePair<string, string>> GetUrlContent(string code) =>
    [
        new("av", "0"),
        new("__d", "www"),
        new("__user", "0"),
        new("__a", "1"),
        new("__req", "3"),
        new("__hs", "19624.HYP:instagram_web_pkg.2.1..0.0"),
        new("dpr", "3"),
        new("__ccg", "UNKNOWN"),
        new("__rev", "1008824440"),
        new("__s", "xf44ne:zhh75g:xr51e7"),
        new("__hsi", "7282217488877343271"),
        new("__dyn",
            "7xeUmwlEnwn8K2WnFw9-2i5U4e0yoW3q32360CEbo1nEhw2nVE4W0om78b87C0yE5ufz81s8hwGwQwoEcE7O2l0Fwqo31w9a9x-0z8-U2zxe2GewGwso88cobEaU2eUlwhEe87q7-0iK2S3qazo7u1xwIw8O321LwTwKG1pg661pwr86C1mwraCg"),
        new("__csr",
            "gZ3yFmJkillQvV6ybimnG8AmhqujGbLADgjyEOWz49z9XDlAXBJpC7Wy-vQTSvUGWGh5u8KibG44dBiigrgjDxGjU0150Q0848azk48N09C02IR0go4SaR70r8owyg9pU0V23hwiA0LQczA48S0f-x-27o05NG0fkw"),
        new("__comet_req", "7"),
        new("lsd", "AVqbxe3J_YA"),
        new("jazoest", "2957"),
        new("__spin_r", "1008824440"),
        new("__spin_b", "trunk"),
        new("__spin_t", "1695523385"),
        new("fb_api_caller_class", "RelayModern"),
        new("fb_api_req_friendly_name", "PolarisPostActionLoadPostQueryQuery"),
        new("variables",
            """{"shortcode":"{coder}","fetch_comment_count":"null","fetch_related_profile_media_count":"null","parent_comment_count":"null","child_comment_count":"null","fetch_like_count":"null","fetch_tagged_user_count":"null","fetch_preview_comment_count":"null","has_threaded_comments":"false","hoisted_comment_id":"null","hoisted_reply_id":"null"}"""
                .Replace("{coder}", code)),
        new("server_timestamps", "true"),
        new("doc_id", "10015901848480474")
    ];

    [GeneratedRegex("/reel/([^/?]+)")]
    private static partial Regex MyRegex();
}
