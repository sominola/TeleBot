namespace TeleBot.Lib.Models;

public class Document
{
    public required long ChatId { get; set; }
    public required string Video { get; set; } = null!;
    public bool? HasSpoiler { get; set; }
    public bool? DisableNotification { get; set; }
    public int? ReplyToMessageId { get; set; }
}
