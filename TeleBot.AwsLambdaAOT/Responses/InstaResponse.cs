using System.Text.Json.Serialization;

namespace TeleBot.AwsLambdaAOT.Responses;

public class InstaResponse
{
    [JsonPropertyName("data")]
    public InstaData Data { get; set; } = null!;
}

public class InstaData
{
    [JsonPropertyName("xdt_shortcode_media")]
    public ShortCodeMedia ShortCodeMedia { get; set; } = null!;
}

public class ShortCodeMedia
{
    [JsonPropertyName("video_url")]
    public string VideoUrl { get; set; } = null!;
}
