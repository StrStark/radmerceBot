namespace radmerceBot.Core.Models;

public class SuperUser
{
    public long Id { get; set; }

    public long TelegramUserId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string PhoneNumber { get; set; } = null!;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
