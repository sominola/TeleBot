using TeleBot.Lib.Models.Enums;

namespace TeleBot.Lib.Models;

public class Message
{
    public int MessageId { get; set; }
    public TeleUser? From { get; set; }
    public Chat Chat { get; set; } = default!;
    public string? Text { get; set; }
    public MessageEntity[]? Entities { get; set; }

    public MessageType Type => Text is null ? MessageType.Unknown : MessageType.Text;
}

public class MessageEntity
{
    public string Type { get; set; } = null!;
    public int Length { get; set; }
    public int Offset { get; set; }
}
