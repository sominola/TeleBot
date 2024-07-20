using System.Text.Json.Serialization;

namespace TeleBot.AwsLambdaAOT.Responses;


public class InstsaResponse
{
    [JsonPropertyName("data")]
    public string Data { get; set; } = null!;
}
