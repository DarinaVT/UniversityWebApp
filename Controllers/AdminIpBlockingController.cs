using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace UniWebApp.Controllers;

[Authorize(Roles = "Admin")]
public class AdminIpBlockingController : Controller
{
    private readonly IIpBlockingService _ipBlockingService;
    private readonly IBlacklistService _blacklistService;
    private readonly ILogger<AdminIpBlockingController> _logger;

    public AdminIpBlockingController(
        IIpBlockingService ipBlockingService,
        IBlacklistService blacklistService,
        ILogger<AdminIpBlockingController> logger)
    {
        _ipBlockingService = ipBlockingService;
        _blacklistService = blacklistService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        var blockedIps = _ipBlockingService.GetBlockedIps().ToList();
        return View(blockedIps);
    }

    [HttpPost]
    public IActionResult BlockIp(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return BadRequest("IP address is required");
        }

        _ipBlockingService.BlockIp(ipAddress.Trim());
        _logger.LogInformation("Admin {User} blocked IP: {Ip}", User.Identity?.Name, ipAddress);
        
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult UnblockIp(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return BadRequest("IP address is required");
        }

        _ipBlockingService.UnblockIp(ipAddress.Trim());
        _logger.LogInformation("Admin {User} unblocked IP: {Ip}", User.Identity?.Name, ipAddress);
        
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public IActionResult Reload()
    {
        _ipBlockingService.Reload();
        _logger.LogInformation("Admin {User} reloaded blocked IPs list", User.Identity?.Name);
        
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult GetMyIp()
    {
        var clientIp = GetClientIpAddress();
        return Json(new { ip = clientIp });
    }

    [HttpPost]
    public async Task<IActionResult> CheckBlacklist(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return BadRequest("IP address is required");
        }

        var result = await _blacklistService.CheckIpAsync(ipAddress.Trim());
        return Json(result);
    }

    [HttpPost]
    public async Task<IActionResult> CheckAndBlockIfBlacklisted(string ipAddress)
    {
        if (string.IsNullOrWhiteSpace(ipAddress))
        {
            return BadRequest("IP address is required");
        }

        ipAddress = ipAddress.Trim();
        var result = await _blacklistService.CheckIpAsync(ipAddress);

        if (result.IsBlacklisted)
        {
            _ipBlockingService.BlockIp(ipAddress);
            _logger.LogInformation("Admin {User} auto-blocked IP {Ip} because it's blacklisted on: {Blacklists}",
                User.Identity?.Name, ipAddress, string.Join(", ", result.BlacklistedOn));
        }

        return Json(new
        {
            isBlacklisted = result.IsBlacklisted,
            blacklistedOn = result.BlacklistedOn,
            autoBlocked = result.IsBlacklisted
        });
    }

    private string? GetClientIpAddress()
    {
        var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedFor))
        {
            var ips = forwardedFor.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (ips.Length > 0)
            {
                return ips[0];
            }
        }

        var realIp = Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
        {
            return realIp;
        }

        var remoteIp = HttpContext.Connection.RemoteIpAddress;
        if (remoteIp != null)
        {
            if (remoteIp.IsIPv4MappedToIPv6)
            {
                return remoteIp.MapToIPv4().ToString();
            }
            return remoteIp.ToString();
        }

        return null;
    }
}

