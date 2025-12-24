using Microsoft.Extensions.Configuration;
using radmerceBot.Core.Interfaces;
using radmerceBot.Infrastructure.Sms;

namespace radmerceBot.Infrastructure.Services;

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
}
