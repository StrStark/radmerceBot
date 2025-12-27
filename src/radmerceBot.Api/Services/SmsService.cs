using radmerceBot.Api.Interfaces;
using radmerceBot.Api.Sms;

namespace radmerceBot.Api.Services;

public class SmsService : ISmsService
{
    private readonly ippanelService _ippanelService;
    private readonly string _fromNumber;

    public SmsService(
        ippanelService ippanelService,
        IConfiguration configuration)
    {
        _ippanelService = ippanelService;
        _fromNumber = configuration["Sms:FromNumber"]!;
    }
    public async Task SendOtp(
            string phone,
            string code,
            CancellationToken cancellationToken)
    {
        var message = $"کد تایید شما: {code}";

        await _ippanelService.SendSmsAsync(
            fromNumber: _fromNumber,
            message: message,
            mobile: phone
        );
    }
    public async Task SendSMS(
            string phone,
            string Message,
            CancellationToken cancellationToken)
    {

            await _ippanelService.SendSmsAsync(
            fromNumber: _fromNumber,
            message: Message,
            mobile: phone
        );
    }
}
