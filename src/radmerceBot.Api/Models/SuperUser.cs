using radmerceBot.Api.Enums;
using System.ComponentModel.DataAnnotations;

namespace radmerceBot.Api.Models;

public class SuperUser
{
    [Key]
    public Guid Id { get; set; }

    public long TelegramUserId { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string PhoneNumber { get; set; } = null!;

    public bool IsActive { get; set; } = true;
    public SuperUserState State { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? TempData { get; set; }
}
