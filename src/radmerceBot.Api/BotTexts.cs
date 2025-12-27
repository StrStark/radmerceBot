using radmerceBot.Core.Models;
using Telegram.Bot.Types;

namespace radmerceBot.Api;

public static class BotTexts
{
    public static string UserNotFround { get; set; } = "کاربر مورد نظر پیدا نشد.";
    public static string UserDeletedSuccessfully(string fullname)=> $"کاربر {fullname} با موفقیت حذف شد.";
    public static string InBotSendingMessageUserSelectedTextReq { get; set; } = $"پیام شما برای کاربر انتخاب شده آماده است. لطفاً متن پیام را وارد کنید:";
    public static string SmsSendingMessageUserSelectedTextReq { get; set; } = "پیامک شما برای کاربر انتخاب شده آماده است. لطفاً متن پیامک را وارد کنید:";
    public static string ThereIsNoVideo { get; set; } = "هنوز هیچ ویدیویی اضافه نشده است.";
    public static string WatchedAllVideos { get; set; } = "🎉 شما تمام ویدیوهای رایگان را مشاهده کردید!\nآیا مایل هستید دوره‌های پولی ما را خریداری کنید؟";
    public static string NoVideoFoundWithThisId { get; set; } = "ویدیویی با این شناسه پیدا نشد.";
    public static string VideoWithCaptionOrderDeleted(string Order, string Caption) => $"✅ ویدیوی با Order {Order} و عنوان '{Caption}' حذف شد.";
    public static string SuperUserWellcome { get; set; } = "سلام SuperUser 👋\nبه پنل مدیریت ربات خوش آمدید. یکی از گزینه‌ها را انتخاب کنید:";
    public static string SelectPublicSendingMessageText { get; set; } = "لطفاً متن پیام عمومی را وارد کنید:";
    public static string CsvFileGenerating { get; set; } = "در حال آماده‌سازی فایل CSV ...";
    public static string MobilePhoneNumberSendingRequest { get; set; } = "لطفاً شماره یا نام کاربر مورد نظر را بدون صفر وارد کنید:";
    public static string SmsSendingTypeSelection { get; set; } = "نوع ارسال پیامک را انتخاب کنید:";
    public static string VideosButtonClicked { get; set; } = "لیست ویدیوها (یکی را انتخاب کنید):";
    public static string PleaseSelectOneOfTheAvalableButtons { get; set; } = "لطفاً یکی از گزینه‌های موجود را انتخاب کنید.";
    public static string NoUserFoundWithThisDescription { get; set; } = "هیچ کاربری با این مشخصات پیدا نشد.";
    public static string UserInformationTextSchema(string fullname , string phonenumber , bool IsVerifide)=> $"نام: {fullname}\nشماره: {phonenumber}\nوضعیت احراز هویت: {(IsVerifide ? "✅" : "❌")}";
    public static string EnterDestinationNumber { get; set; } = "شماره مقصد را وارد کنید:";
    public static string SendCsvFile { get; set; } = "لطفاً فایل CSV را ارسال کنید:";
    public static string ReturnToDashboard { get; set; } = "بازگشت به داشبورد";
    public static string EnterSmsText { get; set; } = "متن پیام را وارد کنید:";
    public static string EnterValidNumber { get; set; } = "لطفا شماره معتبر وارد کنید.";
    public static string WrongCsvFileFormat { get; set; } = "فرمت نامعتبر است. فقط فایل CSV ارسال کنید.";
    public static string BulkSmsSendingSomthingWentWrong(string Problem)=> $"❌ اشتباهی رخ داده است\n\n{Problem}";
    public static string BuilSmsProssecedSending { get; set; } = "✅ فایل CSV دریافت و پردازش شد.\nپیامک‌ها در حال ارسال هستند...";
    public static string VideoListChooseOne { get; set; } = "لیست ویدیوها (یکی را انتخاب کنید):";
    public static string SendDesiredVideo { get; set; } = "لطفاً ویدیوی مورد نظر را ارسال کنید:";
    public static string VedioReceavedSendCaption { get; set; } = "ویدیو دریافت شد.\nحالا کپشن ویدیو را ارسال کنید:";
    public static string InvlidVideoFormat { get; set; } = "لطفا یک فایل ویدیو ارسال کنید.\nفرمت‌های معتبر: MP4 و ویدیوهای تلگرام.";
    public static string VideoFileNotFound { get; set; } = "خطا: فایل ویدیو پیدا نشد. لطفاً دوباره اقدام کنید.";
    public static string VideoRecivedManageButtonProvided { get; set; } = "ویدیو ذخیره شد.\n\nبرای مدیریت ویدیوها دکمه 🎥 ویدیوها را بزنید.";
    public static string CaptionCantBeEmpty { get; set; } = "کپشن معتبر وارد کنید.\nکپشن نمی‌تواند خالی باشد.";
    public static string NoPendimgMessageFound { get; set; } = "هیچ پیام در انتظار یافت نشد.";
    public static string MessageSentSuccessfully { get; set; } = "پیام با موفقیت ارسال شد.";
    public static string NoPendingSmsFound { get; set; } = "هیچ پیامک در انتظار یافت نشد.";
    public static string SmsSentSuccessfully { get; set; } = "پیامک با موفقیت ارسال شد.";
    public static string PublicMessageSentSuccessfully { get; set; } = "پیام عمومی با موفقیت ارسال شد.";
    public static string MessageTextCantBeEmpty { get; set; } = "متن پیام نمی‌تواند خالی باشد.";
    public static string UserWellcomeMessage { get; set; } = "تجارت دیگه پیچیده نیست 😎\r\n\r\n🎁 مینی دوره رایگان تجارت از نقطه صفر  هدیه تیم رادمرس به شماست\r\n\r\n✅ برای دریافت دوره به صورت رایگان و شروع تجارت ابتدا نام و نام خانوادگی خود را به صورت متن فارسی اضافه کنید";
    public static string PleaaseSendNameAndFamily { get; set; } = "لطفاً نام و نام خانوادگی خود را به فارسی ارسال کنید.";
    public static string PLeaseSendYourPhoneNumber { get; set; } = "لطفاً شماره تلفن خود را ارسال کنید";
    public static string PLeaseSendYourNumberOnlyUsingButton { get; set; } = "لطفا شماره خود را به صورت 98912******* ارسال کنید\n و یا از دکمه ارسال شماه تلفن استفاده کنید!.";
    public static string TokenSent { get; set; } = "🔐 کد تایید برای شما ارسال شد.\nلطفاً کد را وارد کنید:";
    public static string InvalidToken { get; set; } = "❌ کد وارد شده نامعتبر یا منقضی شده است.";
    public static string AuthorizationCompleted { get; set; } = $"احراز هویت شما با موفقیت انجام شد ✅\r\nدسترسی به ویدیو ها برای شما فعال شد 🎁";
    public static string DoYouWantOurPaidCoursees { get; set; } = "تبریک میگم، شما تمام ویدیو هارا مشاهده کردید 🏆\r\n\r\nتجارت خود را با تیم رادمرس شروع کنید! اگر میخواهید تجارت را به صورت کامل و حرفه ای در کنار یک تیم حرفه ای با بیش از 20 سال تجربه شروع کنید میتوانید در بوت کمپ جامع تجارت مکاتک پرایم شرکت کنید 😎\r\n\r\nآیا مایل به شرکت در بوت کمپ هستی؟";
    public static string RequestSentForConsultaition { get; set; } = " ✅ درخواست شما ثبت شد! به زودی با شما تماس خواهیم گرفت.\r\nو از طریق لینک زیر میتوانید محصول مورد نظر را مشاهده کنید ";
    public static string ConsultationRequestRejected { get; set; } = "متوجه شدم! موفق و پیروز باشید 😎";
}
