using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace UniWebApp.Controllers;

public class LocalizationController : Controller
{
    [HttpPost]
    public IActionResult Set(string culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return BadRequest();

        if (culture != "en" && culture != "bg")
            culture = "en";

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(
                new RequestCulture(culture)),
            new CookieOptions 
            { 
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                Path = "/"
            }
        );

        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
            return Json(new { success = true, culture = culture });
        }

        var referer = Request.Headers["Referer"].ToString();
        if (string.IsNullOrEmpty(referer))
            referer = "/";

        return Redirect(referer);
    }
}
    