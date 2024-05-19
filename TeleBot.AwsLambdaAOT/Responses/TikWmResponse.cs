using System.Text.Json.Serialization;

namespace TeleBot.AwsLambdaAOT.Responses;

public class TikWmResponse
{
    [JsonPropertyName("data")]
    public TikWmData Data { get; set; } = null!;
}

public class TikWmData
{
    [JsonPropertyName("play")]
    public string Play { get; set; } = null!;

    [JsonPropertyName("images")]
    public string[] Images { get; set; } = [];

    [JsonPropertyName("duration")]
    public double? Duration { get; set; }
    
    [JsonPropertyName("music")]
    public string Music { get; set; } = null!;
    
    [JsonPropertyName("title")]
    public string Title { get; set; } = null!;
}
