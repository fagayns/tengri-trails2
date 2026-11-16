using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace TengriTrails.Services;

public sealed record TelegramUpdate(long UpdateId, long? ChatId, string? Text);

public sealed class TelegramClient(HttpClient http, IOptions<TelegramOptions> options, ILogger<TelegramClient> logger)
{
    private readonly TelegramOptions _options = options.Value;

    public bool Enabled => _options.Enabled;
    public long? AdminChatId => _options.ChatId;

    private string Api(string method) => $"https://api.telegram.org/bot{_options.BotToken}/{method}";

    /// <summary>Sends an HTML message. Never throws: a Telegram outage must not break a booking.</summary>
    public async Task<bool> SendMessageAsync(long chatId, string html, CancellationToken cancellationToken = default)
    {
        if (!Enabled)
            return false;
        try
        {
            using var response = await http.PostAsJsonAsync(
                Api("sendMessage"),
                new { chat_id = chatId, text = html, parse_mode = "HTML", disable_web_page_preview = true },
                cancellationToken);
            if (response.IsSuccessStatusCode)
                return true;

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning("Telegram sendMessage failed with {Status}: {Body}", (int)response.StatusCode, body);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogWarning("Telegram sendMessage failed: {Error} {Message}", ex.GetType().Name, ex.Message);
            return false;
        }
    }

    public async Task<bool> NotifyAdminAsync(string html, CancellationToken cancellationToken = default)
    {
        if (!Enabled || AdminChatId is not { } chatId)
        {
            logger.LogWarning("Telegram BotToken/ChatId are not configured; booking notification skipped.");
            return false;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));
        return await SendMessageAsync(chatId, html, timeout.Token);
    }

    public async Task DeleteWebhookAsync(CancellationToken cancellationToken)
    {
        using var response = await http.PostAsync(Api("deleteWebhook"), null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<TelegramUpdate>> GetUpdatesAsync(long offset, CancellationToken cancellationToken)
    {
        var url = Api("getUpdates") + $"?timeout=25&offset={offset}&allowed_updates=%5B%22message%22%5D";
        using var response = await http.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Telegram getUpdates returned HTTP {(int)response.StatusCode}.");

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var document = JsonDocument.Parse(json);
        var updates = new List<TelegramUpdate>();
        foreach (var item in document.RootElement.GetProperty("result").EnumerateArray())
        {
            var updateId = item.GetProperty("update_id").GetInt64();
            if (item.TryGetProperty("message", out var message)
                && message.TryGetProperty("chat", out var chat)
                && message.TryGetProperty("text", out var text))
                updates.Add(new TelegramUpdate(updateId, chat.GetProperty("id").GetInt64(), text.GetString()));
            else
                updates.Add(new TelegramUpdate(updateId, null, null));
        }

        return updates;
    }
}

public static class TelegramMessages
{
    private static string E(string? value) => WebUtility.HtmlEncode(value ?? "");

    public static string NewBooking(
        string number, string tourName, DateTime date, string time, int adults, int children, decimal total,
        string firstName, string lastName, string email, string phone, string citizenship,
        string messenger, string? comment, string language)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"🆕 <b>Новая заявка {E(number)}</b>");
        sb.AppendLine($"🏔 {E(tourName)}");
        sb.AppendLine($"📅 {date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)} {E(time)}");
        sb.AppendLine($"👥 {adults} взр. + {children} дет. · {total.ToString("N0", CultureInfo.InvariantCulture)} KGS");
        sb.AppendLine();
        sb.AppendLine($"👤 {E(firstName)} {E(lastName)} ({E(citizenship)})");
        sb.AppendLine($"✉️ {E(email)}");
        sb.AppendLine($"📞 {E(phone)} · {E(messenger)}");
        sb.AppendLine($"🌐 язык сайта: {E(language)}");
        if (!string.IsNullOrWhiteSpace(comment))
            sb.AppendLine($"💬 {E(comment)}");
        return sb.ToString();
    }

    public static string BookingLine(
        string? number, string status, string? tourName, DateTime startUtc, int adults, int children,
        decimal total, string firstName, string lastName, string email, string phone, string messenger)
    {
        var local = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc).AddHours(6);
        return $"<b>{E(number)}</b> · {E(status)}\n"
            + $"{E(tourName)} · {local.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}\n"
            + $"{E(firstName)} {E(lastName)} · {E(phone)} ({E(messenger)})\n"
            + $"{E(email)}\n"
            + $"{adults}+{children} · {total.ToString("N0", CultureInfo.InvariantCulture)} KGS";
    }
}
