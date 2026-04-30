using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Services;

public class BlacklistService : IBlacklistService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BlacklistService> _logger;
    private readonly List<string> _dnsblServers;

    public BlacklistService(
        IHttpClientFactory httpClientFactory,
        ILogger<BlacklistService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        _dnsblServers = new List<string>
        {
            "zen.spamhaus.org",           
            "bl.spamcop.net",             
            "dnsbl.sorbs.net",            
            "spam.dnsbl.anonmails.de",    
            "dnsbl-1.uceprotect.net",     
            "dnsbl-2.uceprotect.net",     
            "dnsbl-3.uceprotect.net",     
            "b.barracudacentral.org",     
            "bl.deadbeef.com",            
            "dnsbl.dronebl.org",          
            "rbl.efnetrbl.org",           
            "noptr.spamrats.com",         
            "dnsbl.spfbl.net",            
            "bl.blocklist.de",             
            "all.s5h.net",                
            "all.spamrats.com",           
            "bogons.cymru.com",           
            "cbl.abuseat.org",            
            "cdl.anti-spam.org.cn",        
            "dnsbl.ahbl.org",              
            "dnsbl.anticaptcha.net",       
            "dnsbl.cyberlogic.net",        
            "dnsbl.justspam.org",          
            "dnsbl.kempt.net",             
            "dnsbl.net.ua",                
            "dnsbl.solid.net",             
            "dnsbl.tornevall.org",        
            "dul.dnsbl.sorbs.net",         
            "dyna.spamrats.com",           
            "http.dnsbl.sorbs.net",        
            "misc.dnsbl.sorbs.net",        
            "smtp.dnsbl.sorbs.net",        
            "socks.dnsbl.sorbs.net",       
            "spam.dnsbl.sorbs.net",        
            "web.dnsbl.sorbs.net",         
            "zombie.dnsbl.sorbs.net",      
            "blackholes.five-ten-sg.com",  
            "blacklist.woody.ch",          
            "bogons.cymru.com",            
            "cbl.abuseat.org",             
            "cdl.anti-spam.org.cn",        
            "combined.abuse.ch",            
            "db.wpbl.info",                
            "dnsbl-1.uceprotect.net",      
            "dnsbl-2.uceprotect.net",      
            "dnsbl-3.uceprotect.net",      
            "dnsbl.ahbl.org",             
            "dnsbl.anticaptcha.net",       
            "dnsbl.cyberlogic.net",        
            "dnsbl.justspam.org",          
            "dnsbl.kempt.net",             
            "dnsbl.net.ua",                
            "dnsbl.solid.net",             
            "dnsbl.tornevall.org",         
            "dul.dnsbl.sorbs.net",         
            "dyna.spamrats.com",           
            "http.dnsbl.sorbs.net",        
            "misc.dnsbl.sorbs.net",        
            "smtp.dnsbl.sorbs.net",        
            "socks.dnsbl.sorbs.net",       
            "spam.dnsbl.sorbs.net",        
            "web.dnsbl.sorbs.net",         
            "zombie.dnsbl.sorbs.net"       
        };
    }

    public async Task<BlacklistCheckResult> CheckIpAsync(string ipAddress)
    {
        var result = new BlacklistCheckResult
        {
            IpAddress = ipAddress
        };

        if (string.IsNullOrWhiteSpace(ipAddress) || !IPAddress.TryParse(ipAddress, out var ip))
        {
            result.ErrorMessage = "Invalid IP address";
            return result;
        }

        try
        {
            var reversedIp = ReverseIpAddress(ip);
            
            var blacklistedOn = new List<string>();
            var tasks = new List<Task<string?>>();

            foreach (var dnsblServer in _dnsblServers)
            {
                tasks.Add(CheckDnsblAsync(reversedIp, dnsblServer));
            }

            var results = await Task.WhenAll(tasks);
            
            foreach (var (dnsblResult, index) in results.Select((r, i) => (r, i)))
            {
                if (dnsblResult != null)
                {
                    blacklistedOn.Add(_dnsblServers[index]);
                }
            }

            result.IsBlacklisted = blacklistedOn.Count > 0;
            result.BlacklistedOn = blacklistedOn;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking IP {Ip} against blacklists", ipAddress);
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    public async Task<List<BlacklistCheckResult>> CheckIpsAsync(IEnumerable<string> ipAddresses)
    {
        var tasks = ipAddresses.Select(ip => CheckIpAsync(ip));
        return (await Task.WhenAll(tasks)).ToList();
    }

    private async Task<string?> CheckDnsblAsync(string reversedIp, string dnsblServer)
    {
        try
        {
            var query = $"{reversedIp}.{dnsblServer}";
            
            
            var addresses = await Dns.GetHostAddressesAsync(query);
            
            if (addresses.Length > 0)
            {
                return dnsblServer;
            }
        }
        catch (System.Net.Sockets.SocketException)
        {
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error checking DNSBL {Dnsbl} for {ReversedIp}", dnsblServer, reversedIp);
            return null;
        }

        return null;
    }

    private string ReverseIpAddress(IPAddress ip)
    {
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
        {
            var bytes = ip.GetAddressBytes();
            return $"{bytes[3]}.{bytes[2]}.{bytes[1]}.{bytes[0]}";
        }
        else if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            var bytes = ip.GetAddressBytes();
            var reversed = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                reversed[i] = bytes[15 - i];
            }
            return string.Join(".", reversed.Select(b => b.ToString("x2")));
        }

        throw new ArgumentException("Unsupported address family");
    }
}

