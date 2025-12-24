public class PhoneOtp
{
    public long Id { get; set; }

    public string PhoneNumber { get; set; } = null!;

    public string Code { get; set; } = null!;

    public DateTime ExpireAt { get; set; }

    public bool IsUsed { get; set; }
}
