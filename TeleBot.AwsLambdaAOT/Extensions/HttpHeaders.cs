namespace TeleBot.AwsLambdaAOT.Extensions;

public static class HttpHeaders
{
    public static class Insta
    {
        public static readonly Dictionary<string, string> Headers = new()
        {
            { "X-FB-Friendly-Name", "PolarisPostActionLoadPostQueryQuery" },
            { "X-CSRFToken", "RVDUooU5MYsBbS1CNN3CzVAuEP8oHB52" },
            { "X-IG-App-ID", "1217981644879628" },
            { "X-FB-LSD", "AVqbxe3J_YA" },
            { "X-ASBD-ID", "129477" },
            { "Sec-Fetch-Dest", "empty" },
            { "Sec-Fetch-Mode", "cors" },
            { "Sec-Fetch-Site", "same-origin" },
            {
                "User-Agent",
                "Mozilla/5.0 (Linux; Android 11; SAMSUNG SM-G973U) AppleWebKit/537.36 (KHTML, like Gecko) SamsungBrowser/14.2 Chrome/87.0.4280.141 Mobile Safari/537.36"
            }
        };

        public static readonly List<KeyValuePair<string, string>> UrlContent =
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
            new("server_timestamps", "true"),
            new("doc_id", "10015901848480474")
        ];

        public static KeyValuePair<string, string> GetContentPostId(string postId)
        {
            return new KeyValuePair<string, string>("variables",
                $"{{\"shortcode\":\"{postId}\",\"fetch_comment_count\":null,\"fetch_related_profile_media_count\":null,\"parent_comment_count\":null,\"child_comment_count\":null,\"fetch_like_count\":null,\"fetch_tagged_user_count\":null,\"fetch_preview_comment_count\":null,\"has_threaded_comments\":false,\"hoisted_comment_id\":null,\"hoisted_reply_id\":null}}");
        }
    }
}
