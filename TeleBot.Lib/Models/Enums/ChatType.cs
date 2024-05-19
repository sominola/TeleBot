using System.Text.Json.Serialization;

namespace TeleBot.Lib.Models.Enums;

[JsonConverter(typeof(JsonStringEnumConverter<ChatType>))]
public enum ChatType
{
    Private = 1,
    Group,
    Channel,
    Supergroup,
    Sender
}
