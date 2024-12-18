using System.Text.Json.Serialization;

namespace TeleBot.AwsLambdaAOT.Responses;

public class YoutubeUrlRequest
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = null!;
}

public class YoutubeUrlResponse
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = null!;
}

public class YoutubeSessionResponse
{
    [JsonPropertyName("token")]
    public string Token { get; set; } = null!;
}
