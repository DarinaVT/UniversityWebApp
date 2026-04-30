using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Services;

public class IpBlockingService : IIpBlockingService
{
    private readonly ILogger<IpBlockingService> _logger;
    private readonly string _blockedIpsFilePath;
    private readonly ConcurrentDictionary<string, bool> _blockedIps = new();
    private readonly ConcurrentDictionary<string, IPNetwork> _blockedCidrRanges = new();
    private readonly object _lock = new object();

    public IpBlockingService(ILogger<IpBlockingService> logger, IWebHostEnvironment environment)
    {
        _logger = logger;
        _blockedIpsFilePath = Path.Combine(
            environment.ContentRootPath,
            "wwwroot",
            "data",
            "blocked-ips.json"
        );
        
        LoadBlockedIps();
    }

    public bool IsBlocked(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return false;

        ipAddress = ipAddress.Trim();

        if (_blockedIps.ContainsKey(ipAddress))
        {
            _logger.LogDebug("IP {IpAddress} found in blocked IPs list", ipAddress);
            return true;
        }

        if (IPAddress.TryParse(ipAddress, out var ip))
        {
            foreach (var cidrRange in _blockedCidrRanges.Values)
            {
                if (cidrRange.Contains(ip))
                {
                    _logger.LogDebug("IP {IpAddress} found in blocked CIDR range", ipAddress);
                    return true;
                }
            }
        }

        return false;
    }

    public void BlockIp(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return;

        if (ipAddress.Contains('/'))
        {
            if (TryParseCidr(ipAddress, out var network))
            {
                _blockedCidrRanges.TryAdd(ipAddress, network);
                SaveBlockedIps();
                _logger.LogInformation("Blocked CIDR range: {Cidr}", ipAddress);
            }
        }
        else
        {
            if (IPAddress.TryParse(ipAddress, out _))
            {
                _blockedIps.TryAdd(ipAddress, true);
                SaveBlockedIps();
                _logger.LogInformation("Blocked IP address: {Ip}", ipAddress);
            }
        }
    }

    public void UnblockIp(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
            return;

        if (ipAddress.Contains('/'))
        {
            _blockedCidrRanges.TryRemove(ipAddress, out _);
        }
        else
        {
            _blockedIps.TryRemove(ipAddress, out _);
        }

        SaveBlockedIps();
        _logger.LogInformation("Unblocked IP address: {Ip}", ipAddress);
    }

    public IEnumerable<string> GetBlockedIps()
    {
        var result = new List<string>();
        result.AddRange(_blockedIps.Keys);
        result.AddRange(_blockedCidrRanges.Keys);
        return result;
    }

    public void Reload()
    {
        LoadBlockedIps();
    }

    private void LoadBlockedIps()
    {
        lock (_lock)
        {
            _blockedIps.Clear();
            _blockedCidrRanges.Clear();

            if (!File.Exists(_blockedIpsFilePath))
            {
                _logger.LogWarning("Blocked IPs file not found at {Path}. Creating default file.", _blockedIpsFilePath);
                CreateDefaultBlockedIpsFile();
                return;
            }

            try
            {
                var json = File.ReadAllText(_blockedIpsFilePath);
                var data = JsonSerializer.Deserialize<BlockedIpsData>(json);

                if (data?.BlockedIps != null)
                {
                    foreach (var ip in data.BlockedIps)
                    {
                        if (ip.Contains('/'))
                        {
                            if (TryParseCidr(ip, out var network))
                            {
                                _blockedCidrRanges.TryAdd(ip, network);
                            }
                            else
                            {
                                _logger.LogWarning("Invalid CIDR range: {Cidr}", ip);
                            }
                        }
                        else
                        {
                            var normalizedIp = ip.Trim();
                            
                            if (IPAddress.TryParse(normalizedIp, out var parsedIp))
                            {
                                if (parsedIp.Equals(IPAddress.IPv6Loopback))
                                {
                                    normalizedIp = "127.0.0.1";
                                }
                                else if (parsedIp.IsIPv4MappedToIPv6)
                                {
                                    normalizedIp = parsedIp.MapToIPv4().ToString();
                                }
                                
                                _blockedIps.TryAdd(normalizedIp, true);
                            }
                            else
                            {
                                _logger.LogWarning("Invalid IP address: {Ip}", ip);
                            }
                        }
                    }
                }

                _logger.LogInformation("Loaded {Count} blocked IP addresses and {CidrCount} CIDR ranges", 
                    _blockedIps.Count, _blockedCidrRanges.Count);
                
                if (_blockedIps.Count > 0 || _blockedCidrRanges.Count > 0)
                {
                    var allBlocked = new List<string>();
                    allBlocked.AddRange(_blockedIps.Keys);
                    allBlocked.AddRange(_blockedCidrRanges.Keys);
                    _logger.LogInformation("Blocked IPs/Ranges: {BlockedIps}", string.Join(", ", allBlocked));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading blocked IPs from {Path}", _blockedIpsFilePath);
            }
        }
    }

    private void SaveBlockedIps()
    {
        try
        {
            var data = new BlockedIpsData
            {
                BlockedIps = GetBlockedIps().ToList(),
                LastUpdated = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            var directory = Path.GetDirectoryName(_blockedIpsFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(_blockedIpsFilePath, json);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving blocked IPs to {Path}", _blockedIpsFilePath);
        }
    }

    private void CreateDefaultBlockedIpsFile()
    {
        var data = new BlockedIpsData
        {
            BlockedIps = new List<string>(),
            LastUpdated = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var directory = Path.GetDirectoryName(_blockedIpsFilePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(_blockedIpsFilePath, json);
    }

    private bool TryParseCidr(string cidr, out IPNetwork network)
    {
        network = null!;
        try
        {
            var parts = cidr.Split('/');
            if (parts.Length != 2)
                return false;

            if (!IPAddress.TryParse(parts[0], out var ip))
                return false;

            if (!int.TryParse(parts[1], out var prefixLength))
                return false;

            network = new IPNetwork(ip, prefixLength);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private class BlockedIpsData
    {
        public List<string> BlockedIps { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    private class IPNetwork
    {
        private readonly IPAddress _networkAddress;
        private readonly int _prefixLength;
        private readonly byte[] _networkBytes;
        private readonly byte[] _maskBytes;

        public IPNetwork(IPAddress address, int prefixLength)
        {
            _networkAddress = address;
            _prefixLength = prefixLength;

            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                _networkBytes = address.GetAddressBytes();
                _maskBytes = CalculateMask(prefixLength, 4);
            }
            else if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                _networkBytes = address.GetAddressBytes();
                _maskBytes = CalculateMask(prefixLength, 16);
            }
            else
            {
                throw new ArgumentException("Unsupported address family");
            }
        }

        public bool Contains(IPAddress address)
        {
            if (address.AddressFamily != _networkAddress.AddressFamily)
                return false;

            var addressBytes = address.GetAddressBytes();
            if (addressBytes.Length != _networkBytes.Length)
                return false;

            for (int i = 0; i < _networkBytes.Length; i++)
            {
                if ((addressBytes[i] & _maskBytes[i]) != (_networkBytes[i] & _maskBytes[i]))
                    return false;
            }

            return true;
        }

        private byte[] CalculateMask(int prefixLength, int bytesLength)
        {
            var mask = new byte[bytesLength];
            for (int i = 0; i < bytesLength; i++)
            {
                if (prefixLength >= 8)
                {
                    mask[i] = 0xFF;
                    prefixLength -= 8;
                }
                else if (prefixLength > 0)
                {
                    mask[i] = (byte)(0xFF << (8 - prefixLength));
                    prefixLength = 0;
                }
                else
                {
                    mask[i] = 0x00;
                }
            }
            return mask;
        }
    }
}

