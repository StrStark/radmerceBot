using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace radmerceBot.Api.Telegram;

public class TelegramBotService : ITelegramBotService
{
    private readonly TelegramBotClient _botClient;

    public TelegramBotService(string botToken)
    {
        _botClient = new TelegramBotClient(botToken);
    }

    public Task SendTextMessageAsync(long chatId, string message)
    {
        return _botClient.SendMessage(chatId, message);
    }
    public async Task SendTextMessageAsync(long chatId, string message, ReplyMarkup replyMarkup)
    {
        await _botClient.SendMessage(chatId: chatId, text: message, replyMarkup: replyMarkup);
    }

}