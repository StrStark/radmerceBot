using System.ComponentModel.DataAnnotations;

namespace radmerceBot.Api.Models;

public class FreeVideo
{
    [Key]
    public Guid Id { get; set; }
    public string? FileId { get; set; }
    public string? Caption { get; set; }
    public int Order { get; set; }
}