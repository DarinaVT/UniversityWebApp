namespace Services;

public interface IBlacklistService
{
    Task<BlacklistCheckResult> CheckIpAsync(string ipAddress);

    Task<List<BlacklistCheckResult>> CheckIpsAsync(IEnumerable<string> ipAddresses);
}

public class BlacklistCheckResult
{
    public string IpAddress { get; set; } = string.Empty;
    public bool IsBlacklisted { get; set; }
    public List<string> BlacklistedOn { get; set; } = new();
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public string? ErrorMessage { get; set; }
}

