using TeleBot.Lib.Models.Enums;

namespace TeleBot.Lib.Models;

public class Chat
{
    public long Id { get; set; }
    public string Title { get; set; } = null!;

    public ChatType Type { get; set; }
}
