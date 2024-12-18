using System.Text.Json.Serialization;

namespace TeleBot.AwsLambdaAOT.Responses;

public class InstagramMediaResponse
{
    [JsonPropertyName("data")]
    public InstagramData Data { get; set; }
}

public class InstagramData
{
    [JsonPropertyName("xdt_shortcode_media")]
    public InstagramXdt InstagramXdt { get; set; }
}

public class InstagramXdt
{
    
    [JsonPropertyName("video_url")]
    public string VideoUrl { get; set; }
}
