namespace TeleBot.Lib.Models;

public class TeleUser
{
    public long Id { get; set; }
    public bool IsBot { get; set; }
    public string FirstName { get; set; } = default!;
    public string? LastName { get; set; }
    public string? Username { get; set; }
    public bool? IsPremium { get; set; }
}
