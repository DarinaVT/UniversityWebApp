namespace Services;

public interface IIpBlockingService
{
    bool IsBlocked(string ipAddress);

    void BlockIp(string ipAddress);

    void UnblockIp(string ipAddress);

    IEnumerable<string> GetBlockedIps();

    void Reload();
}

