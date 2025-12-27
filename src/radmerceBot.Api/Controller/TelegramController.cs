using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using radmerceBot.Api.Data;
using radmerceBot.Api.Enums;
using radmerceBot.Api.Exceptions;
using radmerceBot.Api.Interfaces;
using radmerceBot.Api.Models;
using radmerceBot.Api.Services;
using radmerceBot.Api.TelegramService;
using radmerceBot.Core.Models;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
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

        SuperUser superUser;

        Int64 chatId = 0;
        if (update.CallbackQuery != null)
        {
            chatId = update.CallbackQuery.Message!.Chat.Id;
            superUser = await _db.SuperUsers.FirstOrDefaultAsync(su => su.TelegramUserId == chatId) ?? new SuperUser();
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
                    BotTexts.InBotSendingMessageUserSelectedTextReq 
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
                    BotTexts.SmsSendingMessageUserSelectedTextReq 
                );

                superUser.State = SuperUserState.SendingSmsToUser;
                await _db.SaveChangesAsync();
            }
            else if (data.StartsWith("delete:"))
            {
                Console.WriteLine("Deleting");
                var userId = Guid.Parse(data.Split(':')[1]);
                var targetUser = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId);

                if (targetUser == null)
                {
                    await _telegram.SendTextMessageAsync(chatId, BotTexts.UserNotFround);
                }
                _db.Users.Remove(targetUser);
                await _db.SaveChangesAsync();
                await _telegram.SendTextMessageAsync(chatId, BotTexts.UserDeletedSuccessfully(targetUser.FullName!));

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
                    await _telegram.SendTextMessageAsync(chatId, BotTexts.ThereIsNoVideo);
                }
                if (User.CurrentFreeVideoIndex >= videos.Count)
                {
                    User.CurrentFreeVideoIndex = 0;
                    User.CompletedFreeVideoCycles++;
                    User.Step = UserStep.OfferedPaidCourse;
                    await _db.SaveChangesAsync();
                    var offerKeyboard = new ReplyKeyboardMarkup(
                    [
                            [new KeyboardButton("✅ بله، می‌خواهم دوره را بخرم")],
                            [new KeyboardButton("❌ نه، بعداً")]
                    ]    )
                    {
                        ResizeKeyboard = true,
                        OneTimeKeyboard = true
                    };

                    await _telegram.SendTextMessageAsync(
                        chatId
                        ,BotTexts.WatchedAllVideos,
                        offerKeyboard
                    );
                }
                else
                {
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
                    
            }
            else if (data.StartsWith("delvideo:"))
            {
                var videoId = Guid.Parse(data.Split(':')[1]);
                var video = await _db.FreeVideos.FirstOrDefaultAsync(v => v.Id == videoId);
                if (video == null)
                {
                    await _telegram.SendTextMessageAsync(chatId, BotTexts.NoVideoFoundWithThisId);
                }

                _db.FreeVideos.Remove(video);
                await _db.SaveChangesAsync();
                if (callbackQuery.Message != null)
                {
                    await _telegram.DeleteMessageAsync(callbackQuery.Message.Chat.Id, callbackQuery.Message.MessageId);
                }
                await _telegram.SendTextMessageAsync(chatId, BotTexts.VideoWithCaptionOrderDeleted(video.Order.ToString() , video.Caption!));
            }
        }

        if (update.Type != UpdateType.Message)
            return Ok();
        var message = update.Message!;
        chatId = message.Chat.Id;
        var FirstName = message.Chat.FirstName;
        var LastName = message.Chat.LastName;
        var TellId  = message.Chat.Username;
        superUser = await _db.SuperUsers.FirstOrDefaultAsync(su => su.TelegramUserId == chatId);

        if (superUser != null)
        {
            switch (superUser.State)
            {
                case SuperUserState.None:


                    await _telegram.SendTextMessageAsync(
                        chatId
                        , BotTexts.SuperUserWellcome,
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
                            await _telegram.SendTextMessageAsync(chatId, BotTexts.SelectPublicSendingMessageText);
                            break;
                        case "💾 خروجی CSV از کاربران و درخواست‌ها":
                            {
                                superUser.State = SuperUserState.ExportingCsv;
                                await _db.SaveChangesAsync();

                                await _telegram.SendTextMessageAsync(chatId, BotTexts.CsvFileGenerating);

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
                            await _telegram.SendTextMessageAsync(chatId, BotTexts.MobilePhoneNumberSendingRequest , new ReplyKeyboardMarkup());
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
                                BotTexts.SmsSendingTypeSelection,
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
                                BotTexts.VideosButtonClicked,
                                manageVideosKeyboard
                            );
                            break;


                        default:
                            await _telegram.SendTextMessageAsync(chatId, BotTexts.PleaseSelectOneOfTheAvalableButtons , superUserKeyboard);
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
                        await _telegram.SendTextMessageAsync(chatId, BotTexts.NoUserFoundWithThisDescription , superUserKeyboard);
                        superUser.State = SuperUserState.Dashboard;
                        await _db.SaveChangesAsync();
                        break;
                    }

                    foreach (var u in matchedUsers)
                    {
                        var inlineKeyboard = new InlineKeyboardMarkup(
                        [
                            [InlineKeyboardButton.WithCallbackData(" ارسال پیام در ربات", $"bot:{u.Id}")],
                            [InlineKeyboardButton.WithCallbackData("📩 ارسال پیامک", $"sms:{u.Id}")],
                            [InlineKeyboardButton.WithCallbackData("❌ حذف مخاطب", $"delete:{u.Id}")]
                        ]);

                        string userInfo = BotTexts.UserInformationTextSchema(u.FullName , u.PhoneNumber , u.IsPhoneVerified);
                        await _telegram.SendTextMessageAsync(chatId, userInfo, inlineKeyboard);
                    }

                    superUser.State = SuperUserState.Dashboard;
                    await _db.SaveChangesAsync();
                    break;              

                case SuperUserState.SendingSms_Menu:
                    switch (message.Text)
                    {
                        case "📨 ارسال پیامک تکی":
                            superUser.State = SuperUserState.SendingSms_Single_WaitingForPhone;
                            await _db.SaveChangesAsync();
                            await _telegram.SendTextMessageAsync(chatId, BotTexts.EnterDestinationNumber);
                            break;

                        case "📂 ارسال پیامک گروهی (CSV)":
                            superUser.State = SuperUserState.SendingSms_Bulk_WaitingForFile;
                            await _db.SaveChangesAsync();
                            await _telegram.SendTextMessageAsync(chatId, BotTexts.SendCsvFile);
                            break;

                        case "⬅️ بازگشت به داشبورد":
                            superUser.State = SuperUserState.Dashboard;
                            await _db.SaveChangesAsync();
                            await _telegram.SendTextMessageAsync(chatId, BotTexts.ReturnToDashboard, superUserKeyboard);
                            break;

                        default:
                            var smsMenuKeyboard = new ReplyKeyboardMarkup(
                            [
                                [new KeyboardButton("📨 ارسال پیامک تکی")],
                                [new KeyboardButton("📂 ارسال پیامک گروهی (CSV)")],
                                [new KeyboardButton("⬅️ بازگشت به داشبورد")]

                            ])
                            {
                                ResizeKeyboard = true
                            };
                            await _telegram.SendTextMessageAsync(chatId, BotTexts.PleaseSelectOneOfTheAvalableButtons , smsMenuKeyboard);
                            break;
                    }
                    break;

                case SuperUserState.SendingSms_Single_WaitingForPhone:
                    if (!string.IsNullOrWhiteSpace(message.Text) && IsValidPhone(message.Text))
                    {
                        superUser.TempData = message.Text.Trim();
                        superUser.State = SuperUserState.SendingSms_Single_WaitingForMessage;
                        await _db.SaveChangesAsync();
                        await _telegram.SendTextMessageAsync(chatId, BotTexts.EnterSmsText);
                    }
                    else
                    {
                        await _telegram.SendTextMessageAsync(chatId, BotTexts.EnterValidNumber);
                    }
                    break;

                case SuperUserState.SendingSms_Bulk_WaitingForFile:
                    if (message.Document != null)
                    {
                        var fileName = message.Document.FileName;
                        if (!fileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                        {
                            await _telegram.SendTextMessageAsync(chatId,
                                BotTexts.WrongCsvFileFormat);
                            break;
                        }

                        var fileStream = await _telegram.GetFileAsync(message.Document.FileId);

                        var isValid = await BulkSmsProcessor(fileStream , cancellationToken : HttpContext.RequestAborted);

                        superUser.State = SuperUserState.Dashboard;
                        await _db.SaveChangesAsync();
                        if (!isValid.Item1)
                        {
                            await _telegram.SendTextMessageAsync(chatId,
                                BotTexts.BulkSmsSendingSomthingWentWrong(isValid.Item2));
                        }
                        else
                        {
                            await _telegram.SendTextMessageAsync(chatId, BotTexts.BuilSmsProssecedSending);
                        }
                        
                    }
                    else
                    {
                        await _telegram.SendTextMessageAsync(chatId, BotTexts.SendCsvFile);
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
                                await _telegram.SendTextMessageAsync(chatId, BotTexts.ThereIsNoVideo);
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
                                chatId
                                ,BotTexts.VideoListChooseOne,
                                manageVideosKeyboard
                            );
                            break;


                        case "➕ افزودن ویدیو":
                            superUser.State = SuperUserState.AddingVideo_WaitingForFile;
                            await _db.SaveChangesAsync();

                            await _telegram.SendTextMessageAsync(chatId,
                                BotTexts.SendDesiredVideo);
                            break;


                        case "⬅️ بازگشت به داشبورد":
                            superUser.State = SuperUserState.Dashboard;
                            await _db.SaveChangesAsync();

                            await _telegram.SendTextMessageAsync(chatId, BotTexts.ReturnToDashboard, superUserKeyboard);
                            break;


                        default:
                            var manageVideosKeyboardawd = new ReplyKeyboardMarkup(
                               [
                                   [new KeyboardButton("📋 مشاهده ویدیوها"), new KeyboardButton("➕ افزودن ویدیو")],
                                   [new KeyboardButton("⬅️ بازگشت به داشبورد")]
                               ]
                           )
                            {
                                ResizeKeyboard = true
                            };
                            await _telegram.SendTextMessageAsync(chatId,
                                BotTexts.PleaseSelectOneOfTheAvalableButtons , manageVideosKeyboardawd);
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
                            BotTexts.VedioReceavedSendCaption
                        );
                    }
                    else
                    {
                        await _telegram.SendTextMessageAsync(
                            chatId,
                            BotTexts.InvlidVideoFormat
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
                                BotTexts.VideoFileNotFound, superUserKeyboard);

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
                            BotTexts.VideoRecivedManageButtonProvided,
                            superUserKeyboard
                        );
                    }
                    else
                    {
                        await _telegram.SendTextMessageAsync(
                            chatId,
                            BotTexts.CaptionCantBeEmpty
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
                        await _telegram.SendTextMessageAsync(chatId, BotTexts.NoPendimgMessageFound);
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

                    await _telegram.SendTextMessageAsync(chatId, BotTexts.MessageSentSuccessfully);
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
                        await _telegram.SendTextMessageAsync(chatId, BotTexts.NoPendingSmsFound);
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
                        await _smsService.SendSMS(TargetUser.PhoneNumber, smsText, HttpContext.RequestAborted);
                    }

                    await _telegram.SendTextMessageAsync(chatId, BotTexts.SmsSentSuccessfully);
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

                        await _telegram.SendTextMessageAsync(chatId, BotTexts.PublicMessageSentSuccessfully);
                    }
                    else
                    {
                        await _telegram.SendTextMessageAsync(chatId, BotTexts.MessageTextCantBeEmpty);
                    }

                    superUser.State = SuperUserState.Dashboard;
                    await _db.SaveChangesAsync();
                    break;

                case SuperUserState.SendingSms_Single_WaitingForMessage:
                    string Text = message.Text?.Trim();
                    if (string.IsNullOrEmpty(Text))
                        break;

                    await _smsService.SendSMS($"{superUser.TempData}", Text, HttpContext.RequestAborted);
                    

                    await _telegram.SendTextMessageAsync(chatId, BotTexts.SmsSentSuccessfully , superUserKeyboard);
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
                    chatId,BotTexts.UserWellcomeMessage
                    
                );

                user.Step = UserStep.WaitingForFullName;
                await _db.SaveChangesAsync();
                break;

            case UserStep.WaitingForFullName:
                if (string.IsNullOrWhiteSpace(message.Text))
                {
                    await _telegram.SendTextMessageAsync(
                        chatId,
                        BotTexts.PleaaseSendNameAndFamily
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
                    BotTexts.PLeaseSendYourPhoneNumber,
                    phoneKeyboard
                );

                user.Step = UserStep.WaitingForPhone;
                await _db.SaveChangesAsync();
                break;

            case UserStep.WaitingForPhone: // add checking it the way they sent there number is the right way ... 
                if (message.Contact == null || message.Contact.UserId != chatId)
                {
                    if (!IsValidPhone(message.Text!.Trim()))
                    {
                        var phoneKeyboard23 = new ReplyKeyboardMarkup(
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
                            BotTexts.PLeaseSendYourNumberOnlyUsingButton,
                            phoneKeyboard23
                        );
                    }

                }

                user.PhoneNumber = message.Contact!.PhoneNumber ?? message.Text!.Trim() ;

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

                var phoneKeyboard2 = new ReplyKeyboardMarkup([
                        [KeyboardButton.WithRequestContact("اصلاح شماره تماس")],
                        [KeyboardButton.WithRequestContact("ارسال مجدد کد")]

                    ])
                {
                    ResizeKeyboard = true,
                    OneTimeKeyboard = true
                };


                await _telegram.SendTextMessageAsync(
                    chatId,
                    BotTexts.TokenSent , phoneKeyboard2
                );
                break;

            case UserStep.WaitingForOtp:
                if (string.IsNullOrWhiteSpace(message.Text))
                    break;
                if(message.Text == "اصلاح شماره تماس")
                {
                    var phoneKeyboard3 = new ReplyKeyboardMarkup(
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
                        BotTexts.PLeaseSendYourPhoneNumber,
                        phoneKeyboard3
                    );
                    user.Step = UserStep.WaitingForPhone;
                    await _db.SaveChangesAsync();
                    break;
                }
                else if (message.Text == "ارسال مجدد کد")
                {
                    otpCode = Random.Shared.Next(100000, 999999).ToString();

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

                    phoneKeyboard2 = new ReplyKeyboardMarkup([
                            [KeyboardButton.WithRequestContact("اصلاح شماره تماس")],
                        [KeyboardButton.WithRequestContact("ارسال مجدد کد")]

                        ])
                    {
                        ResizeKeyboard = true,
                        OneTimeKeyboard = true
                    };


                    await _telegram.SendTextMessageAsync(
                        chatId,
                        BotTexts.TokenSent, phoneKeyboard2
                    );
                    break;
                }
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
                        BotTexts.InvalidToken
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
                    BotTexts.AuthorizationCompleted,
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
                        await _telegram.SendTextMessageAsync(chatId, BotTexts.ThereIsNoVideo);
                        break;
                    }

                    int index = user.CurrentFreeVideoIndex;

                    if (index >= videos.Count)
                    {
                        user.CurrentFreeVideoIndex = 0; // بازنشانی برای مشاهده مجدد
                        user.CompletedFreeVideoCycles++;
                        user.Step = UserStep.OfferedPaidCourse; // پیشنهاد دوره‌های پولی
                        await _db.SaveChangesAsync();
                        var offerKeyboard = new ReplyKeyboardMarkup([
                            [new KeyboardButton("✅ بله، می‌خواهم دوره را بخرم")],
                            [new KeyboardButton("❌ نه، بعداً")]
                        ])
                        {
                            ResizeKeyboard = true,
                            OneTimeKeyboard = true
                        };

                        await _telegram.SendTextMessageAsync(
                            chatId,
                            BotTexts.DoYouWantOurPaidCoursees,
                            offerKeyboard
                        );
                    }
                    else
                    {
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

                        
                }
                else
                {
                    var freeVideoKeyboardawd = new ReplyKeyboardMarkup(
                    new[]
                    {
                        new KeyboardButton("🎥 مشاهده ویدیوهای رایگان")
                    })
                    {
                        ResizeKeyboard = true
                    };
                    await _telegram.SendTextMessageAsync(chatId, BotTexts.PleaseSelectOneOfTheAvalableButtons , freeVideoKeyboardawd);
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

                        user.Step = UserStep.Registered; // بازگشت به حالت ثبت‌نام شده
                        await _db.SaveChangesAsync();
                        var freeVideoKeyboards = new ReplyKeyboardMarkup(
                            new[]
                            {
                                 new KeyboardButton("🎥 مشاهده ویدیوهای رایگان")
                            })
                        {
                            ResizeKeyboard = true
                        };
                        await _telegram.SendTextMessageAsync(
                            chatId,
                            BotTexts.RequestSentForConsultaition
                         , freeVideoKeyboards);
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
                            BotTexts.ConsultationRequestRejected
                            , freeVideoKeyboard
                        );
                        break;

                    default:
                        freeVideoKeyboard = new ReplyKeyboardMarkup(
                       new[]
                       {
                             new KeyboardButton("🎥 مشاهده ویدیوهای رایگان")
                       })
                        {
                            ResizeKeyboard = true
                        };
                        await _telegram.SendTextMessageAsync(
                            chatId,
                            BotTexts.PleaseSelectOneOfTheAvalableButtons
                        , freeVideoKeyboard);
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
    async Task<(bool ,string)> BulkSmsProcessor(Stream File , CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(File, Encoding.UTF8);

        string? headerLine = await reader.ReadLineAsync();
        if (headerLine == null)
            return (false , "");

        var expectedHeader = "Number,Text";
        if (!string.Equals(headerLine.Trim(), expectedHeader, StringComparison.OrdinalIgnoreCase))
            return (false, "Format Not Correct!");

        var smsItems = new List<(string Number, string Text)>();

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var parts = line.Split(',', 2); // فقط به دو بخش تقسیم شود
            if (parts.Length != 2)
                return (false, "Format Not Correct!");

            var number = parts[0].Trim();
            var text = parts[1].Trim();

            if (string.IsNullOrWhiteSpace(number) || string.IsNullOrWhiteSpace(text))
                return (false, "Empty Row Item");
            
            smsItems.Add((number, text));
            Console.WriteLine(number , text);
        }

        foreach (var item in smsItems)
        {
            try
            {
                await _smsService.SendSMS(item.Number, item.Text, cancellationToken);
                Console.WriteLine(item.Number, item.Text);
            }
            catch (TooManyRequestsException ex)
            {

                return (false, "برای ارسال مجدد 10 دقیقه صبر کنید");
            }
            catch  (Exception ex) 
            {
                return (false, ex.Message);

            }

        }

        return (true,"Completed");
    }

}
