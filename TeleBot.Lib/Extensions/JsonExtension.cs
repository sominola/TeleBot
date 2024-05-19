using System.Text.Json.Serialization;
using TeleBot.Lib.Models;

namespace TeleBot.Lib.Extensions;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
[JsonSerializable(typeof(Update))]
[JsonSerializable(typeof(TeleUser))]
[JsonSerializable(typeof(TeleResult))]
[JsonSerializable(typeof(Message))]
[JsonSerializable(typeof(Document))]
[JsonSerializable(typeof(Chat))]
public partial class TeleGenerationContext : JsonSerializerContext
{
}
