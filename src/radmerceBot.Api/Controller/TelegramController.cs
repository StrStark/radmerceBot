using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using radmerceBot.Api.Data;
using radmerceBot.Api.Enums;
using radmerceBot.Api.Interfaces;
using radmerceBot.Api.Models;
using radmerceBot.Api.Telegram;
using radmerceBot.Core.Models;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace radmerceBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TelegramController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ISmsService _smsService;
    private readonly ITelegramBotService _telegram;

    public TelegramController(
        AppDbContext db,
        ITelegramBotService telegram,
        ISmsService smsService)
    {
        _db = db;
        _telegram = telegram;
        _smsService = smsService;
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> Webhook([FromBody] Update update)
    {
        if (update.Type != UpdateType.Message)
            return Ok();

        var message = update.Message!;
        var chatId = message.Chat.Id;

        var user = await _db.Users.FirstOrDefaultAsync(u => u.TelegramUserId == chatId);
        if (user == null)
        {
            user = new Core.Models.User
            {
                TelegramUserId = chatId,
                Step = UserStep.Start,
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        switch (user.Step)
        {
            case UserStep.Start:

                var replyMarkup = new ReplyKeyboardMarkup(
                    new[]
                    {
                        KeyboardButton.WithRequestContact("ارسال شماره من")
                    })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };

                await _telegram.SendTextMessageAsync(chatId,
                    "سلام! خوش آمدید به ربات آموزشی ما.\nلطفاً شماره تلفن خود را ارسال کنید.",
                    replyMarkup);

                user.Step = UserStep.WaitingForPhone;
                await _db.SaveChangesAsync();
                break;

            case UserStep.WaitingForPhone:
                if (message.Contact != null && message.Contact.UserId == chatId)
                {
                    user.PhoneNumber = message.Contact.PhoneNumber;

                    var otpCode = Random.Shared.Next(100000, 999999).ToString();

                    var phoneotp = new PhoneOtp
                    {
                        PhoneNumber = user.PhoneNumber,
                        Code = otpCode,
                        ExpireAt = DateTime.UtcNow.AddMinutes(2),
                        IsUsed = false
                    };

                    _db.PhoneOtps.Add(phoneotp);

                    await _smsService.SendOtp(
                        user.PhoneNumber,
                        otpCode,
                        HttpContext.RequestAborted
                    );
                    user.Step = UserStep.WaitingForOtp;

                    await _db.SaveChangesAsync();

                    await _telegram.SendTextMessageAsync(
                        chatId,
                        "کد تایید برای شماره شما ارسال شد.\nلطفاً کد را وارد کنید:"
                    );
                }
                else
                {
                    await _telegram.SendTextMessageAsync(
                        chatId,
                        "لطفاً شماره خود را فقط از طریق دکمه ارسال کنید."
                    );
                }
                break;
            case UserStep.WaitingForOtp:
                if (string.IsNullOrWhiteSpace(message.Text))
                    break;

                var otp = await _db.PhoneOtps
                    .Where(x =>
                        x.PhoneNumber == user.PhoneNumber &&
                        x.Code == message.Text &&
                        !x.IsUsed &&
                        x.ExpireAt > DateTime.UtcNow)
                    .OrderBy(x => x.ExpireAt)
                    .FirstOrDefaultAsync();

                if (otp == null)
                {
                    await _telegram.SendTextMessageAsync(
                        chatId,
                        "کد وارد شده نامعتبر یا منقضی شده است."
                    );
                    break;
                }

                otp.IsUsed = true;
                user.Step = UserStep.Registered;

                await _db.SaveChangesAsync();

                await _telegram.SendTextMessageAsync(
                    chatId,
                    "✅ شماره شما با موفقیت تایید شد. خوش آمدید!"
                );

                break;


            case UserStep.Registered:
                await _telegram.SendTextMessageAsync(chatId, "شما قبلاً ثبت‌نام کرده‌اید.");
                break;
        }

        return Ok();
    }
}
