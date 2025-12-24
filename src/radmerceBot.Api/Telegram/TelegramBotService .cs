using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace radmerceBot.Api.TelegramService;

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

    public Task SendVideoByFileIdAsync(long chatId, string fileId, string caption , ReplyMarkup replyMarkup)
    {
        var video = new InputFileId(fileId);
        return _botClient.SendVideo(
            chatId: chatId,
            video: video,
            caption: caption
            ,replyMarkup : replyMarkup
        );
    }
    public Task SendVideoAsync(long chatId, Stream videoStream, string fileName, string caption, ReplyMarkup replyMarkup)
    {
        var video = new InputFileStream(videoStream, fileName);

        return _botClient.SendVideo(
            chatId: chatId,
            video: video,
            caption: caption,
            replyMarkup: replyMarkup
        );
    }
    public async Task<Stream> GetFileAsync(string fileId)
    {
        var tgFile = await _botClient.GetFile(fileId);
        var ms = new MemoryStream();
        await _botClient.DownloadFile(tgFile.FilePath!, ms);
        ms.Position = 0;
        return ms;
    }
    public async Task DeleteMessageAsync(long chatId, int messageId)
    {
        try
        {
            await _botClient.DeleteMessage(chatId, messageId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting message {messageId} in chat {chatId}: {ex.Message}");
        }
    }
    public async Task SendFileAsync(long chatId, Stream fileStream, string fileName, string caption)
    {
        await _botClient.SendDocument(chatId, new InputFileStream(fileStream, fileName), caption: caption);
    }

}