using Telegram.Bot.Types.ReplyMarkups;

namespace radmerceBot.Api.TelegramService;

public interface ITelegramBotService
{
    Task SendTextMessageAsync(long chatId, string message);
    Task SendTextMessageAsync(long chatId,string message,ReplyMarkup replyMarkup);
    Task SendVideoByFileIdAsync(long chatId, string fileId, string caption,ReplyMarkup replyMarkup);
    Task SendVideoAsync(long chatId, Stream videoStream, string fileName, string caption, ReplyMarkup replyMarkup);
    Task<Stream> GetFileAsync(string fileId);
    Task DeleteMessageAsync(long chatId, int messageId);
    Task SendFileAsync(long chatId, Stream fileStream, string fileName, string caption);


}