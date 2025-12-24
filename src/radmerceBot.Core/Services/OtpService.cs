namespace radmerceBot.Core.Services;

public class OtpService
{
    public string GenerateCode()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }

    public DateTime ExpireTime()
    {
        return DateTime.UtcNow.AddMinutes(2);
    }
}
