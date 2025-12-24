using radmerceBot.Api.Enums;

namespace radmerceBot.Core.Models;

public class User
{
    public Guid Id { get; set; }

    public string? TelId { get; set; }
    public long TelegramUserId { get; set; }
    public string? FullName { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? PhoneNumber { get; set; }
    public bool IsPhoneVerified { get; set; }

    public UserStep Step { get; set; }

    public DateTime CreatedAt { get; set; }
}
