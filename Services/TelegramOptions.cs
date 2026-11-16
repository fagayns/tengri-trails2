namespace TengriTrails.Services;

public sealed class TelegramOptions
{
    public string? BotToken { get; set; }
    public long? ChatId { get; set; }
    public bool Enabled => !string.IsNullOrWhiteSpace(BotToken);
}
