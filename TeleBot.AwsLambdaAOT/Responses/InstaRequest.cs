using System.Text.Json.Serialization;

namespace TeleBot.AwsLambdaAOT.Responses;


public class InstaRequest
{
    [JsonPropertyName("iphone")]
    public bool Iphone { get; set; } = false;
    [JsonPropertyName("url")]
    public string Url { get; set; } = null!;
}

public class InstaJobResponse
{
    [JsonPropertyName("job_id")]
    public string JobId { get; set; } = null!;
}

public class InstaResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; }= null!;
    [JsonPropertyName("payload")]
    public InstaPayloadResponse[]? Payload { get; set; }
}

public class InstaPayloadResponse
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = null!;
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;
}
