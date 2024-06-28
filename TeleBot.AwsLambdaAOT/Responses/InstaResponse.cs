using System.Text.Json.Serialization;

namespace TeleBot.AwsLambdaAOT.Responses;

public class FastDlRequest
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = null!;

    [JsonPropertyName("ts")]
    public long Ts { get; set; }

    [JsonPropertyName("_ts")]
    public long _Ts { get; set; }

    [JsonPropertyName("_tsc")]
    public long _Tsc { get; set; }
    
    [JsonPropertyName("_s")]
    public string _S { get; set; } = null!;
}

public class FastDlResponse
{
    [JsonPropertyName("url")]
    public FastDlUrlResponse[] Url { get; set; } = null!;

   
}

public class FastDlUrlResponse
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = null!;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = null!;
    
    [JsonPropertyName("type")]
    public string Type { get; set; } = null!;
    
    [JsonPropertyName("ext")]
    public string Ext { get; set; } = null!;
    
    [JsonPropertyName("quality")]
    public long? Quality { get; set; }
}
