using Microsoft.EntityFrameworkCore;
using TengriTrails.Database;

namespace TengriTrails.Services;

/// <summary>Long-polling bot: /start shows the chat id, /bookings lists the latest requests.</summary>
public sealed class TelegramBotService(
    TelegramClient telegram,
    IServiceScopeFactory scopeFactory,
    ILogger<TelegramBotService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!telegram.Enabled)
        {
            logger.LogInformation("Telegram bot is disabled (Telegram:BotToken is empty).");
            return;
        }

        try
        {
            await telegram.DeleteWebhookAsync(stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning("Could not delete Telegram webhook: {Message}", ex.Message);
        }

        logger.LogInformation("Telegram bot polling started.");
        long offset = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var updates = await telegram.GetUpdatesAsync(offset, stoppingToken);
                foreach (var update in updates)
                {
                    offset = update.UpdateId + 1;
                    if (update.ChatId is { } chatId && update.Text is { } text)
                        await HandleAsync(chatId, text, stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning("Telegram polling failed, retrying in 5 s: {Message}", ex.Message);
                try { await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    private async Task HandleAsync(long chatId, string text, CancellationToken cancellationToken)
    {
        var parts = text.Trim().Split([' ', '@'], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return;

        switch (parts[0].ToLowerInvariant())
        {
            case "/start":
            case "/help":
                await telegram.SendMessageAsync(chatId,
                    "🏔 <b>Tengri Trails</b>\n"
                    + $"Ваш chat id: <code>{chatId}</code>\n\n"
                    + "/bookings — последние 10 заявок\n"
                    + "/help — эта подсказка", cancellationToken);
                break;
            case "/bookings":
                if (telegram.AdminChatId != chatId)
                {
                    await telegram.SendMessageAsync(chatId,
                        "Нет доступа. Укажите этот chat id в настройке Telegram__ChatId.", cancellationToken);
                    break;
                }

                await telegram.SendMessageAsync(chatId, await LatestBookingsAsync(cancellationToken), cancellationToken);
                break;
        }
    }

    private async Task<string> LatestBookingsAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TengriDbContext>();
        var rows = await db.Bookings.AsNoTracking()
            .OrderByDescending(booking => booking.Id)
            .Take(10)
            .Select(booking => new
            {
                booking.BookingNumber,
                booking.Status,
                Tour = booking.Departure.Tour.Translations
                    .Where(translation => translation.Language == "en")
                    .Select(translation => translation.Name)
                    .FirstOrDefault(),
                Start = booking.Departure.StartAtUtc,
                booking.Adults,
                booking.Children,
                booking.TotalAmountKgs,
                booking.FirstName,
                booking.LastName,
                booking.Email,
                booking.Phone,
                booking.Messenger
            })
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return "Заявок пока нет.";

        return "📋 <b>Последние заявки</b>\n\n" + string.Join("\n\n", rows.Select(row =>
            TelegramMessages.BookingLine(row.BookingNumber, row.Status.ToString(), row.Tour, row.Start,
                row.Adults, row.Children, row.TotalAmountKgs, row.FirstName, row.LastName,
                row.Email, row.Phone, row.Messenger)));
    }
}
