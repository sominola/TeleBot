using System.Text.Json.Serialization;

namespace TeleBot.AwsLambdaAOT.Responses;

public class GramPayload
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = null!;

    [JsonPropertyName("ts")]
    public string Timestamp { get; set; } = null!;

    [JsonPropertyName("_ts")]
    public string GramTimestamp { get; set; } = null!;

    [JsonPropertyName("_tsc")]
    public string Tsc { get; set; } = null!;

    [JsonPropertyName("_s")]
    public string Signature { get; set; } = null!;
}

public class GramResponse
{
    [JsonPropertyName("url")]
    public List<GramMedia> Urls { get; set; } = null!;

    [JsonPropertyName("meta")]
    public GramMeta Meta { get; set; } = null!;

    [JsonPropertyName("thumb")]
    public string Thumb { get; set; } = null!;

    [JsonPropertyName("sd")]
    public string Sd { get; set; } = null!;

    [JsonPropertyName("hd")]
    public string Hd { get; set; } = null!;

    [JsonPropertyName("hosting")]
    public string Hosting { get; set; } = null!;

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}

public class GramMedia
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = null!;

    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;

    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;

    [JsonPropertyName("ext")]
    public string Ext { get; set; } = null!;
}

public class GramMeta
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;

    [JsonPropertyName("source")]
    public string Source { get; set; } = null!;

    [JsonPropertyName("shortcode")]
    public string Shortcode { get; set; } = null!;

    [JsonPropertyName("comments")]
    public List<GramComment> Comments { get; set; } = null!;

    [JsonPropertyName("comment_count")]
    public int CommentCount { get; set; }

    [JsonPropertyName("like_count")]
    public int LikeCount { get; set; }

    [JsonPropertyName("taken_at")]
    public long TakenAt { get; set; }

    [JsonPropertyName("username")]
    public string Username { get; set; } = null!;
}

public class GramComment
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;

    [JsonPropertyName("username")]
    public string Username { get; set; } = null!;
}
