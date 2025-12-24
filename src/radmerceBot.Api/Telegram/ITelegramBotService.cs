using Telegram.Bot.Types.ReplyMarkups;

namespace radmerceBot.Api.Telegram;

public interface ITelegramBotService
{
    Task SendTextMessageAsync(long chatId, string message);
    Task SendTextMessageAsync(long chatId,string message,ReplyMarkup replyMarkup);
}