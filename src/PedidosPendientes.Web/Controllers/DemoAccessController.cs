using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using PedidosPendientes.Web.Services;

namespace PedidosPendientes.Web.Controllers;

/// <summary>Pantalla de acceso del entorno DEMO (contraseña única compartida).</summary>
public class DemoAccessController : Controller
{
    private readonly IDataProtectionProvider _dp;
    private readonly IConfiguration _config;

    public DemoAccessController(IDataProtectionProvider dp, IConfiguration config)
    {
        _dp = dp;
        _config = config;
    }

    private bool DemoEnabled => _config.GetValue<bool>("Demo:Enabled");
    private string DemoPassword => _config.GetValue<string>("Demo:Password") ?? string.Empty;

    [HttpGet("/acceso")]
    public IActionResult Acceso(string? returnUrl = null, bool error = false)
    {
        if (!DemoEnabled) return Redirect("/");
        ViewData["ReturnUrl"] = returnUrl;
        ViewData["Error"] = error;
        return View();
    }

    [HttpPost("/acceso")]
    [ValidateAntiForgeryToken]
    public IActionResult Acceso(string? password, string? returnUrl, int _ = 0)
    {
        if (!DemoEnabled) return Redirect("/");

        if (!DemoAccess.PasswordMatches(password, DemoPassword))
            return RedirectToAction(nameof(Acceso), new { returnUrl, error = true });

        Response.Cookies.Append(DemoAccess.CookieName, DemoAccess.IssueToken(_dp), new CookieOptions
        {
            HttpOnly = true,
            Secure = Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromHours(12),
            Path = "/",
        });

        var dest = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl! : "/";
        return Redirect(dest);
    }
}
