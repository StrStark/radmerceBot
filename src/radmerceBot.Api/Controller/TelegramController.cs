using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using radmerceBot.Api.Data;
using radmerceBot.Api.Enums;
using radmerceBot.Api.Interfaces;
using radmerceBot.Api.Models;
using radmerceBot.Api.TelegramService;
using radmerceBot.Core.Models;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
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
        Console.WriteLine("got the webhook");

        if (update.Type == UpdateType.CallbackQuery)
        {

            Console.WriteLine("got CallBack");
        }

        if (update.Type == UpdateType.CallbackQuery)
        {

            Console.WriteLine("got CallBack");
        }

        var superUserKeyboard = new ReplyKeyboardMarkup(
        [
            [new KeyboardButton("🔍 جستجو در مخاطبین"), new KeyboardButton("📤 ارسال پیامک")],
            [new KeyboardButton("🎥 ویدیوها")],
            [new KeyboardButton("💾 خروجی CSV از کاربران و درخواست‌ها")],
            [new KeyboardButton("📣 ارسال پیام به کاربران فعال")]
        ])
        {
            ResizeKeyboard = true
        };
        var message = update.Message!;
        var chatId = message.Chat.Id;
        var superUser = await _db.SuperUsers.FirstOrDefaultAsync(su => su.TelegramUserId == chatId);
        if (update.CallbackQuery != null)
        {
            Console.WriteLine("got CallBack");

            var callbackQuery = update.CallbackQuery!;
            var data = callbackQuery.Data!;
            Console.WriteLine(callbackQuery);
            Console.WriteLine(data);
            if (data.StartsWith("bot:"))
            {
                var userId = Guid.Parse(data.Split(':')[1]);
                var pending = new SuperUserPendingMessage
                {
                    SuperUserId = superUser!.Id,
                    TargetUserId = userId
                };
                _db.PendingMessages.Add(pending);
                await _db.SaveChangesAsync();
                await _telegram.SendTextMessageAsync(
                    chatId,
                    $"پیام شما برای کاربر انتخاب شده آماده است. لطفاً متن پیام را وارد کنید:"
                );
                superUser.State = SuperUserState.SendingMessageToUser;
                await _db.SaveChangesAsync();

            }
            else if (data.StartsWith("sms:"))
            {
                var targetUserId = Guid.Parse(data.Split(':')[1]);

                var pendingSms = new SuperUserPendingSms
                {
                    SuperUserId = superUser.Id,
                    TargetUserId = targetUserId
                };

                _db.PendingSmsMessages.Add(pendingSms);
                await _db.SaveChangesAsync();

                await _telegram.SendTextMessageAsync(
                    chatId,
                    "پیامک شما برای کاربر انتخاب شده آماده است. لطفاً متن پیامک را وارد کنید:"
                );

                superUser.State = SuperUserState.SendingSmsToUser;
                await _db.SaveChangesAsync();
            }
            else if (data.StartsWith("delete:"))
            {
                var userId = Guid.Parse(data.Split(':')[1]);
                var targetUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

                if (targetUser == null)
                {
                    await _telegram.SendTextMessageAsync(chatId, "کاربر مورد نظر پیدا نشد.");
                }
                _db.Users.Remove(targetUser);
                await _db.SaveChangesAsync();
                await _telegram.SendTextMessageAsync(chatId, $"کاربر {targetUser.FullName} با موفقیت حذف شد.");

                superUser.State = SuperUserState.Dashboard;
                await _db.SaveChangesAsync();
            }
            else if (data.StartsWith("nextvideo:"))
            {
                var userId = Guid.Parse(data.Split(':')[1]);
                var User = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);
                var videos = await _db.FreeVideos.OrderBy(v => v.Order).ToListAsync();
                if (!videos.Any())
                {
                    await _telegram.SendTextMessageAsync(chatId, "هنوز هیچ ویدیویی اضافه نشده است.");
                }
                if (User.CurrentFreeVideoIndex >= videos.Count)
                {
                    User.CurrentFreeVideoIndex = 0;
                    User.CompletedFreeVideoCycles++;
                    User.Step = UserStep.OfferedPaidCourse;
                    await _db.SaveChangesAsync();
                    var offerKeyboard = new ReplyKeyboardMarkup(new[]
                    {
                            new KeyboardButton("✅ بله، می‌خواهم دوره را بخرم"),
                            new KeyboardButton("❌ نه، بعداً")
                        })
                    {
                        ResizeKeyboard = true,
                        OneTimeKeyboard = true
                    };

                    await _telegram.SendTextMessageAsync(
                        chatId,
                        "🎉 شما تمام ویدیوهای رایگان را مشاهده کردید!\nآیا مایل هستید دوره‌های پولی ما را خریداری کنید؟",
                        offerKeyboard
                    );
                }
                var currentVideo = videos[User.CurrentFreeVideoIndex];

                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("ویدیو بعدی", $"nextvideo:{User.Id}")
                    }
                });

                await _telegram.SendVideoByFileIdAsync(
                    chatId: User.TelegramUserId,
                    fileId: currentVideo.FileId!,
                    caption: currentVideo.Caption,
                    replyMarkup: inlineKeyboard
                );

                User.CurrentFreeVideoIndex++;
                await _db.SaveChangesAsync();
            }
            else if (data.StartsWith("delvideo:"))
            {
                var videoId = Guid.Parse(data.Split(':')[1]);
                var video = await _db.FreeVideos.FirstOrDefaultAsync(v => v.Id == videoId);
                if (video == null)
                {
                    await _telegram.SendTextMessageAsync(chatId, "ویدیویی با این شناسه پیدا نشد.");
                }

                _db.FreeVideos.Remove(video);
                await _db.SaveChangesAsync();
                if (callbackQuery.Message != null)
                {
                    await _telegram.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
                }
                await _telegram.SendTextMessageAsync(chatId, $"✅ ویدیوی با Order {video.Order} و عنوان '{video.Caption}' حذف شد.");
            }
        }

        if (update.Type != UpdateType.Message)
            return Ok();
        var FirstName = message.Chat.FirstName;
        var LastName = message.Chat.LastName;
        var TellId  = message.Chat.Username;

        if (superUser != null)
        {
            switch (superUser.State)
            {
                case SuperUserState.None:


                    await _telegram.SendTextMessageAsync(
                        chatId,
                        "سلام SuperUser 👋\nبه پنل مدیریت ربات خوش آمدید. یکی از گزینه‌ها را انتخاب کنید:",
                        superUserKeyboard
                    );

                    superUser.State = SuperUserState.Dashboard;
                    await _db.SaveChangesAsync();
                    break;

                case SuperUserState.Dashboard:
                    switch (message.Text)
                    {
                        case "📣 ارسال پیام به کاربران فعال":
                            superUser.State = SuperUserState.SendingMessageToActiveUsers;
                            await _db.SaveChangesAsync();
                            await _telegram.SendTextMessageAsync(chatId, "لطفاً متن پیام عمومی را وارد کنید:");
                            break;
                        case "💾 خروجی CSV از کاربران و درخواست‌ها":
                            {
                                superUser.State = SuperUserState.ExportingCsv;
                                await _db.SaveChangesAsync();

                                await _telegram.SendTextMessageAsync(chatId, "در حال آماده‌سازی فایل CSV ...");

                                var users = await _db.Users.ToListAsync();
                                var usersCsv = new StringBuilder();
                                usersCsv.AppendLine("Id,FullName,PhoneNumber,IsPhoneVerified");
                                foreach (var u in users)
                                {
                                    usersCsv.AppendLine($"{u.Id},{u.FullName},{u.PhoneNumber},{u.IsPhoneVerified}");
                                }
                                var usersBytes = Encoding.UTF8.GetBytes(usersCsv.ToString());
                                using (var usersStream = new MemoryStream(usersBytes))
                                {
                                    await _telegram.SendFileAsync(chatId, usersStream, "Users.csv", "لیست کاربران");
                                }

                                var requests = await _db.RequestedConsultations.ToListAsync();
                                var requestsCsv = new StringBuilder();
                                requestsCsv.AppendLine("Id,UserId,FullName,PhoneNumber,RequestedAt");
                                foreach (var r in requests)
                                {
                                    requestsCsv.AppendLine($"{r.Id},{r.TelegramUserId},{r.FullName},{r.PhoneNumber},{r.RequestedAt:O}");
                                }
                                var requestsBytes = Encoding.UTF8.GetBytes(requestsCsv.ToString());
                                using (var requestsStream = new MemoryStream(requestsBytes))
                                {
                                    await _telegram.SendFileAsync(chatId, requestsStream, "RequestedConsultations.csv", "درخواست‌های ثبت‌شده");
                                }

                                superUser.State = SuperUserState.Dashboard;
                                await _db.SaveChangesAsync();
                                break;
                            }

                        case "🔍 جستجو در مخاطبین":
                            superUser.State = SuperUserState.SearchingContacts;
                            await _db.SaveChangesAsync();
                            await _telegram.SendTextMessageAsync(chatId, "لطفاً شماره یا نام کاربر مورد نظر را بدون صفر وارد کنید:" , new ReplyKeyboardMarkup());
                            break;

                        case "📤 ارسال پیامک":
                            var smsMenuKeyboard = new ReplyKeyboardMarkup(
                            new[]
                            {
                                 new KeyboardButton("📨 ارسال پیامک تکی"),
                                 new KeyboardButton("📂 ارسال پیامک گروهی (CSV)"),
                                 new KeyboardButton("⬅️ بازگشت به داشبورد")
                            })
                            {
                                ResizeKeyboard = true
                            };

                            await _telegram.SendTextMessageAsync(
                                chatId,
                                "نوع ارسال پیامک را انتخاب کنید:",
                                smsMenuKeyboard
                            );

                            superUser.State = SuperUserState.SendingSms_Menu;
                            await _db.SaveChangesAsync();
                            break;

                        case "🎥 ویدیوها":
                            superUser.State = SuperUserState.ManagingVideos;
                            await _db.SaveChangesAsync();

                            var manageVideosKeyboard = new ReplyKeyboardMarkup(
                                [
                                    [new KeyboardButton("📋 مشاهده ویدیوها"), new KeyboardButton("➕ افزودن ویدیو")],
                                    [new KeyboardButton("⬅️ بازگشت به داشبورد")]
                                ]
                            )
                            {
                                ResizeKeyboard = true
                            };

                            await _telegram.SendTextMessageAsync(
                                chatId,
                                "لیست ویدیوها (یکی را انتخاب کنید):",
                                manageVideosKeyboard
                            );
                            break;


                        default:
                            await _telegram.SendTextMessageAsync(chatId, "لطفاً یکی از گزینه‌های موجود را انتخاب کنید." , superUserKeyboard);
                            break;
                    }
                    break;

                case SuperUserState.SearchingContacts:
                    string query = message.Text?.Trim() ?? "";

                    var matchedUsers = await _db.Users
                        .Where(u => u.LastName!.Contains(query) || u.FirstName!.Contains(query) || u.FullName!.Contains(query) || u.PhoneNumber!.Contains(query))
                        .ToListAsync();

                    if (!matchedUsers.Any())
                    {
                        await _telegram.SendTextMessageAsync(chatId, "هیچ کاربری با این مشخصات پیدا نشد." , superUserKeyboard);
                        superUser.State = SuperUserState.Dashboard;
                        await _db.SaveChangesAsync();
                        break;
                    }

                    foreach (var u in matchedUsers)
                    {
                        var inlineKeyboard = new InlineKeyboardMarkup(new[]
                        {
                            new []
                            {
                                InlineKeyboardButton.WithCallbackData(" ارسال پیام در ربات", $"bot:{u.Id}"),
                                InlineKeyboardButton.WithCallbackData("📩 ارسال پیامک", $"sms:{u.Id}"),
                                InlineKeyboardButton.WithCallbackData("❌ حذف مخاطب", $"delete:{u.Id}")

                            }
                        });

                        string userInfo = $"نام: {u.FullName}\nشماره: {u.PhoneNumber}\nوضعیت احراز هویت: {(u.IsPhoneVerified ? "✅" : "❌")}";
                        await _telegram.SendTextMessageAsync(chatId, userInfo, inlineKeyboard);
                    }

                    superUser.State = SuperUserState.Dashboard;
                    await _db.SaveChangesAsync();
                    break;

                case SuperUserState.SendingSms:

                   

                case SuperUserState.SendingSms_Menu:
                    switch (message.Text)
                    {
                        case "📨 ارسال پیامک تکی":
                            superUser.State = SuperUserState.SendingSms_Single_WaitingForPhone;
                            await _db.SaveChangesAsync();
                            await _telegram.SendTextMessageAsync(chatId, "شماره مقصد را وارد کنید:");
                            break;

                        case "📂 ارسال پیامک گروهی (CSV)":
                            superUser.State = SuperUserState.SendingSms_Bulk_WaitingForFile;
                            await _db.SaveChangesAsync();
                            await _telegram.SendTextMessageAsync(chatId, "لطفاً فایل CSV را ارسال کنید:");
                            break;

                        case "⬅️ بازگشت به داشبورد":
                            superUser.State = SuperUserState.Dashboard;
                            await _db.SaveChangesAsync();
                            await _telegram.SendTextMessageAsync(chatId, "بازگشت به داشبورد" , superUserKeyboard);
                            break;

                        default:
                            await _telegram.SendTextMessageAsync(chatId, "گزینه نامعتبر است، دوباره انتخاب کنید.");
                            break;
                    }
                    break;

                case SuperUserState.SendingSms_Single_WaitingForPhone:
                    if (!string.IsNullOrWhiteSpace(message.Text) && IsValidPhone(message.Text))
                    {
                        superUser.TempData = message.Text.Trim();
                        superUser.State = SuperUserState.SendingSms_Single_WaitingForMessage;
                        await _db.SaveChangesAsync();
                        await _telegram.SendTextMessageAsync(chatId, "متن پیام را وارد کنید:");
                    }
                    else
                    {
                        await _telegram.SendTextMessageAsync(chatId, "شماره معتبر وارد کنید.");
                    }
                    break;

                case SuperUserState.SendingSms_Bulk_WaitingForFile:
                    if (message.Document != null)
                    {
                        var fileName = message.Document.FileName;
                        if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                        {
                            await _telegram.SendTextMessageAsync(chatId,
                                "فرمت نامعتبر است. فقط فایل CSV ارسال کنید.");
                            break;
                        }

                        var fileStream = await _telegram.GetFileAsync(message.Document.FileId);

                        bool isValid = await BulkSmsProcessor(fileStream);

                        superUser.State = SuperUserState.Dashboard;
                        await _db.SaveChangesAsync();
                        if (!isValid)
                        {
                            await _telegram.SendTextMessageAsync(chatId,
                                "❌ فرمت اطلاعات داخل فایل CSV اشتباه است. لطفاً بررسی و اصلاح کنید.");
                        }
                        else
                        {
                            await _telegram.SendTextMessageAsync(chatId,
                                "✅ فایل CSV دریافت و پردازش شد.\nپیامک‌ها در حال ارسال هستند...");
                        }
                        
                    }
                    else
                    {
                        await _telegram.SendTextMessageAsync(chatId, "لطفاً یک فایل CSV ارسال کنید:");
                    }
                    break;

                case SuperUserState.ManagingVideos:
                    switch (message.Text)
                    {
                        case "📋 مشاهده ویدیوها":
                            var videos = await _db.FreeVideos
                                .OrderBy(v => v.Order)
                                .ToListAsync();

                            if (!videos.Any())
                            {
                                await _telegram.SendTextMessageAsync(chatId, "هنوز هیچ ویدیویی اضافه نشده است.");
                                break;
                            }

                            foreach (var v in videos)
                            {
                                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                                {
                                    new[]
                                    {
                                        InlineKeyboardButton.WithCallbackData("❌ حذف", $"delvideo:{v.Id}")
                                    }
                                });
                                await _telegram.SendVideoByFileIdAsync(
                                    chatId: chatId,
                                    fileId :v.FileId!,
                                    caption: $"Order: {v.Order}\n{v.Caption}",
                                    replyMarkup: inlineKeyboard
                                );
                            }
                            break;


                        case "➕ افزودن ویدیو":
                            superUser.State = SuperUserState.AddingVideo_WaitingForFile;
                            await _db.SaveChangesAsync();

                            await _telegram.SendTextMessageAsync(chatId,
                                "لطفاً ویدیوی مورد نظر را ارسال کنید:");
                            break;


                        case "⬅️ بازگشت به داشبورد":
                            superUser.State = SuperUserState.Dashboard;
                            await _db.SaveChangesAsync();

                            await _telegram.SendTextMessageAsync(chatId, "بازگشت به داشبورد", superUserKeyboard);
                            break;


                        default:
                            await _telegram.SendTextMessageAsync(chatId,
                                "گزینه نامعتبر است.\nاز دکمه‌های زیر استفاده کنید:");
                            break;
                    }
                    break;

                case SuperUserState.AddingVideo_WaitingForFile:
                    if (message.Video != null)
                    {
                        superUser.TempData = message.Video.FileId;
                        superUser.State = SuperUserState.AddingVideo_WaitingForCaption;
                        await _db.SaveChangesAsync();

                        await _telegram.SendTextMessageAsync(
                            chatId,
                            "ویدیو دریافت شد.\nحالا کپشن ویدیو را ارسال کنید:"
                        );
                    }
                    else
                    {
                        await _telegram.SendTextMessageAsync(
                            chatId,
                            "لطفا یک فایل ویدیو ارسال کنید.\nفرمت‌های معتبر: MP4 و ویدیوهای تلگرام."
                        );
                    }
                    break;
                
                case SuperUserState.AddingVideo_WaitingForCaption:
                    if (!string.IsNullOrWhiteSpace(message.Text))
                    {
                        string caption = message.Text;

                        if (string.IsNullOrWhiteSpace(superUser.TempData))
                        {

                            await _telegram.SendTextMessageAsync(chatId,
                                "خطا: فایل ویدیو پیدا نشد. لطفاً دوباره اقدام کنید.", superUserKeyboard);

                            superUser.State = SuperUserState.Dashboard;
                            await _db.SaveChangesAsync();
                            break;
                        }

                        string fileId = superUser.TempData;
                        int order = await _db.FreeVideos.CountAsync() + 1;

                        var video = new FreeVideo
                        {
                            FileId = fileId,
                            Caption = caption,
                            Order = order
                        };

                        await _db.FreeVideos.AddAsync(video);

                        superUser.TempData = null;
                        superUser.State = SuperUserState.Dashboard;
                        await _db.SaveChangesAsync();


                        await _telegram.SendTextMessageAsync(
                            chatId,
                            $"ویدیو ذخیره شد.\nOrder: {order}\n\nبرای مدیریت ویدیوها دکمه 🎥 ویدیوها را بزنید.",
                            superUserKeyboard
                        );
                    }
                    else
                    {
                        await _telegram.SendTextMessageAsync(
                            chatId,
                            "کپشن معتبر وارد کنید.\nکپشن نمی‌تواند خالی باشد."
                        );
                    }
                    break;

                case SuperUserState.SendingMessageToUser:
                    string messageText = message.Text?.Trim();
                    if (string.IsNullOrEmpty(messageText))
                        break;

                    var pendingMessage = await _db.PendingMessages
                        .Where(p => p.SuperUserId == superUser.Id && !p.IsSent)
                        .OrderBy(p => p.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (pendingMessage == null)
                    {
                        await _telegram.SendTextMessageAsync(chatId, "هیچ پیام در انتظار یافت نشد.");
                        superUser.State = SuperUserState.Dashboard;
                        await _db.SaveChangesAsync();
                        break;
                    }

                    pendingMessage.MessageText = messageText;
                    pendingMessage.IsSent = true;
                    await _db.SaveChangesAsync();

                    var targetUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == pendingMessage.TargetUserId);
                    if (targetUser != null)
                    {
                        await _telegram.SendTextMessageAsync(targetUser.TelegramUserId, messageText);
                    }

                    await _telegram.SendTextMessageAsync(chatId, "پیام با موفقیت ارسال شد.");
                    superUser.State = SuperUserState.Dashboard;
                    await _db.SaveChangesAsync();
                    break;

                case SuperUserState.SendingSmsToUser:
                    string smsText = message.Text?.Trim();
                    if (string.IsNullOrEmpty(smsText))
                        break;

                    var pending = await _db.PendingSmsMessages
                        .Where(p => p.SuperUserId == superUser.Id && !p.IsSent)
                        .OrderBy(p => p.CreatedAt)
                        .FirstOrDefaultAsync();

                    if (pending == null)
                    {
                        await _telegram.SendTextMessageAsync(chatId, "هیچ پیامک در انتظار یافت نشد.");
                        superUser.State = SuperUserState.Dashboard;
                        await _db.SaveChangesAsync();
                        break;
                    }

                    pending.MessageText = smsText;
                    pending.IsSent = true;
                    await _db.SaveChangesAsync();

                    var TargetUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == pending.TargetUserId);
                    if (TargetUser != null)
                    {
                        await _smsService.SendOtp(TargetUser.PhoneNumber, smsText, HttpContext.RequestAborted);
                    }

                    await _telegram.SendTextMessageAsync(chatId, "پیامک با موفقیت ارسال شد.");
                    superUser.State = SuperUserState.Dashboard;
                    await _db.SaveChangesAsync();
                    break;

                case SuperUserState.SendingMessageToActiveUsers:
                    string publicMessage = message.Text?.Trim() ?? "";
                    if (!string.IsNullOrWhiteSpace(publicMessage))
                    {
                        var targetUsers = await _db.Users
                            .Where(u => u.CompletedFreeVideoCycles > 0)
                            .ToListAsync();

                        foreach (var u in targetUsers)
                        {
                            await _telegram.SendTextMessageAsync(u.TelegramUserId, $"📣\n{publicMessage}");
                        }

                        await _telegram.SendTextMessageAsync(chatId, "پیام عمومی با موفقیت ارسال شد.");
                    }
                    else
                    {
                        await _telegram.SendTextMessageAsync(chatId, "متن پیام نمی‌تواند خالی باشد.");
                    }

                    superUser.State = SuperUserState.Dashboard;
                    await _db.SaveChangesAsync();
                    break;

                case SuperUserState.SendingSms_Single_WaitingForMessage:
                    string Text = message.Text?.Trim();
                    if (string.IsNullOrEmpty(Text))
                        break;

                    await _smsService.SendOtp($"{superUser.TempData}", Text, HttpContext.RequestAborted);
                    

                    await _telegram.SendTextMessageAsync(chatId, "پیامک با موفقیت ارسال شد." , superUserKeyboard);
                    superUser.State = SuperUserState.Dashboard;
                    await _db.SaveChangesAsync();

                    break;
            }
            return Ok();
        }
        var user = await _db.Users.FirstOrDefaultAsync(u => u.TelegramUserId == chatId);
        if (user == null)
        {
            user = new Core.Models.User
            {
                TelegramUserId = chatId,
                Step = UserStep.Start,
                CreatedAt = DateTime.UtcNow,
                FirstName = FirstName,
                LastName = LastName,
                TelId = TellId,
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
        }

        switch (user.Step)
        {
            case UserStep.Start:
                await _telegram.SendTextMessageAsync(
                    chatId,
                    "سلام 👋\nبه ربات آموزشی Radmerce خوش آمدید.\n\nلطفاً نام و نام خانوادگی خود را وارد کنید:"
                );

                user.Step = UserStep.WaitingForFullName;
                await _db.SaveChangesAsync();
                break;

            case UserStep.WaitingForFullName:
                if (string.IsNullOrWhiteSpace(message.Text))
                {
                    await _telegram.SendTextMessageAsync(
                        chatId,
                        "لطفاً نام و نام خانوادگی خود را به صورت متنی ارسال کنید."
                    );
                    break;
                }

                user.FullName = message.Text.Trim();

                var phoneKeyboard = new ReplyKeyboardMarkup(
                new[]
                {
                    KeyboardButton.WithRequestContact("📱 ارسال شماره من")
                })
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };

                await _telegram.SendTextMessageAsync(
                    chatId,
                    $"ممنون {user.FullName} 🌱\nحالا لطفاً شماره تلفن خود را ارسال کنید:",
                    phoneKeyboard
                );

                user.Step = UserStep.WaitingForPhone;
                await _db.SaveChangesAsync();
                break;

            case UserStep.WaitingForPhone:
                if (message.Contact == null || message.Contact.UserId != chatId)
                {
                    await _telegram.SendTextMessageAsync(
                        chatId,
                        "لطفاً شماره خود را فقط از طریق دکمه ارسال کنید."
                    );
                    break;
                }

                user.PhoneNumber = message.Contact.PhoneNumber;

                var otpCode = Random.Shared.Next(100000, 999999).ToString();

                _db.PhoneOtps.Add(new PhoneOtp
                {
                    PhoneNumber = user.PhoneNumber,
                    Code = otpCode,
                    ExpireAt = DateTime.UtcNow.AddMinutes(10),
                    IsUsed = false
                });

                await _smsService.SendOtp(
                    user.PhoneNumber,
                    otpCode,
                    HttpContext.RequestAborted
                );

                user.Step = UserStep.WaitingForOtp;
                await _db.SaveChangesAsync();

                await _telegram.SendTextMessageAsync(
                    chatId,
                    "🔐 کد تایید برای شما ارسال شد.\nلطفاً کد را وارد کنید:"
                );
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
                    .FirstOrDefaultAsync();

                if (otp == null)
                {
                    await _telegram.SendTextMessageAsync(
                        chatId,
                        "❌ کد وارد شده نامعتبر یا منقضی شده است."
                    );
                    break;
                }

                otp.IsUsed = true;
                user.IsPhoneVerified = true;
                
                user.Step = UserStep.Registered;

                var freeVideoKeyboard = new ReplyKeyboardMarkup(
                    new[]
                    {
                        new KeyboardButton("🎥 مشاهده ویدیوهای رایگان")
                    })
                {
                    ResizeKeyboard = true
                };

                await _db.SaveChangesAsync();

                await _telegram.SendTextMessageAsync(
                    chatId,
                    $"✅ احراز هویت با موفقیت انجام شد، {user.FullName}\n\nمی‌توانید ویدیوهای رایگان را مشاهده کنید:",
                    freeVideoKeyboard
                );
                break;

            case UserStep.Registered:
                if (message.Text == "🎥 مشاهده ویدیوهای رایگان")
                {
                    var videos = await _db.FreeVideos
                        .OrderBy(v => v.Order)
                        .ToListAsync();

                    if (!videos.Any())
                    {
                        await _telegram.SendTextMessageAsync(chatId, "هنوز هیچ ویدیویی اضافه نشده است.");
                        break;
                    }

                    int index = user.CurrentFreeVideoIndex;

                    if (index >= videos.Count)
                    {
                        user.CurrentFreeVideoIndex = 0; // بازنشانی برای مشاهده مجدد
                        user.CompletedFreeVideoCycles++;
                        user.Step = UserStep.OfferedPaidCourse; // پیشنهاد دوره‌های پولی
                        await _db.SaveChangesAsync();
                        var offerKeyboard = new ReplyKeyboardMarkup(new[]
                        {
                            new KeyboardButton("✅ بله، می‌خواهم دوره را بخرم"),
                            new KeyboardButton("❌ نه، بعداً")
                        })
                        {
                            ResizeKeyboard = true,
                            OneTimeKeyboard = true
                        };

                        await _telegram.SendTextMessageAsync(
                            chatId,
                            "🎉 شما تمام ویدیوهای رایگان را مشاهده کردید!\nآیا مایل هستید دوره‌های پولی ما را خریداری کنید؟",
                            offerKeyboard
                        );
                    }

                    var currentVideo = videos[index];

                    var nextButton = new InlineKeyboardMarkup(new[]
                    {
                        new[] { InlineKeyboardButton.WithCallbackData("🎬 ویدیوی بعدی", $"nextvideo:{user.Id}") }
                    });

                    await _telegram.SendVideoByFileIdAsync(
                        chatId: chatId,
                        fileId: currentVideo.FileId!,
                        caption: currentVideo.Caption,
                        replyMarkup: nextButton
                    );

                    user.CurrentFreeVideoIndex++;
                    await _db.SaveChangesAsync();
                }
                else
                {
                    await _telegram.SendTextMessageAsync(chatId, "پیام نامعتبر، لطفا از دکمه ها برای ارسال پیام استفاده کنید...");
                }
                break;

            case UserStep.OfferedPaidCourse:
                switch (message.Text)
                {
                    case "✅ بله، می‌خواهم دوره را بخرم":

                        var consultation = new RequestedConsultation
                        {
                            TelegramUserId = user.TelegramUserId,
                            FullName = user.FullName!,
                            PhoneNumber = user.PhoneNumber!,
                            RequestedAt = DateTime.UtcNow,
                        };
                        _db.RequestedConsultations.Add(consultation);

                        user.Step = UserStep.RequestedConsultation;
                        await _db.SaveChangesAsync();

                        await _telegram.SendTextMessageAsync(
                            chatId,
                            "✅ درخواست شما ثبت شد! به زودی با شما تماس خواهیم گرفت."
                        );
                        break;

                    case "❌ نه، بعداً":
                        freeVideoKeyboard = new ReplyKeyboardMarkup(
                        new[]
                        {
                             new KeyboardButton("🎥 مشاهده ویدیوهای رایگان")
                        })
                        {
                            ResizeKeyboard = true
                        };
                        user.Step = UserStep.Registered; // بازگشت به حالت ثبت‌نام شده
                        await _db.SaveChangesAsync();

                        await _telegram.SendTextMessageAsync(
                            chatId,
                            "باشه، هر زمان آماده بودید می‌توانید دوره‌های پولی را ببینید."
                            , freeVideoKeyboard
                        );
                        break;

                    default:
                        await _telegram.SendTextMessageAsync(
                            chatId,
                            "لطفاً یکی از گزینه‌های موجود را انتخاب کنید."
                        );
                        break;
                }
                break;

        }

        return Ok();
    }
    bool IsValidPhone(string input)
    {
        var pattern = @"^989\d{9}$";
        return Regex.IsMatch(input, pattern);
    }
    async Task<bool> BulkSmsProcessor(Stream File)
    {
        return true;
    }

}
