using System.Text.Json.Serialization;
using TeleBot.Lib.Models;

namespace TeleBot.Lib.Extensions;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(Update))]
[JsonSerializable(typeof(TeleUser))]
[JsonSerializable(typeof(TeleResult))]
[JsonSerializable(typeof(Message))]
[JsonSerializable(typeof(MessageEntity))]
[JsonSerializable(typeof(Document))]
[JsonSerializable(typeof(Chat))]
[JsonSerializable(typeof(Dictionary<string, string>))]
public partial class TeleGenerationContext : JsonSerializerContext
{
}
