using System.ComponentModel.DataAnnotations;

namespace radmerceBot.Api.Models;

public class SuperUserPendingSms
{
    [Key]
    public Guid Id { get; set; }

    public Guid SuperUserId { get; set; }

    public Guid TargetUserId { get; set; }

    public string? MessageText { get; set; }

    public bool IsSent { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}