using System.ComponentModel.DataAnnotations;

namespace radmerceBot.Api.Models;

public class RequestedConsultation
{
    [Key]
    public Guid Id { get; set; }

    public long TelegramUserId { get; set; }

    public string? FullName { get; set; }

    public string? PhoneNumber { get; set; }

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
}
