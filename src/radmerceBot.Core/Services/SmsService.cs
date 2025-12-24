using radmerceBot.Core.Interfaces;

namespace radmerceBot.Core.Services;

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
    public Task SendOtp(string phone, string code , CancellationToken cancellationToken)
    {
        // use messaging panel
        return Task.CompletedTask;
    }
}
