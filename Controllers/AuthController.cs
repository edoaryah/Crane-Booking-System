using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using AspnetCoreMvcFull.Services.Auth;
using AspnetCoreMvcFull.ViewModels.Auth;
using AspnetCoreMvcFull.Filters;

namespace AspnetCoreMvcFull.Controllers
{
  public class AuthController : Controller
  {
    private readonly IAuthService _authService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthService authService, ILogger<AuthController> logger)
    {
      _authService = authService;
      _logger = logger;
    }

    [HttpGet]
    public IActionResult Login(string returnUrl = "/")
    {
      // Jika pengguna sudah login, redirect ke halaman utama
      if (User.Identity?.IsAuthenticated == true)
      {
        return Redirect(returnUrl);
      }

      ViewData["ReturnUrl"] = returnUrl;
      return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ServiceFilter(typeof(RateLimitFilter))]
    public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = "/")
    {
      ViewData["ReturnUrl"] = returnUrl;

      if (!ModelState.IsValid)
      {
        return View(model);
      }

      try
      {
        var response = await _authService.LoginAsync(model.Username, model.Password);

        if (!response.Success || response.Employee == null)
        {
          ModelState.AddModelError(string.Empty, response.Message);
          return View(model);
        }

        // Create claims identity
        var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, response.Employee.EmpId),
                    new Claim(ClaimTypes.Name, response.Employee.Name),
                    new Claim("ldapuser", response.Employee.LdapUser),
                    new Claim("department", response.Employee.Department),
                    new Claim("division", response.Employee.Division),
                    new Claim("position", response.Employee.PositionTitle),
                    new Claim("email", response.Employee.Email ?? string.Empty),
                    new Claim("auth_time", DateTime.Now.ToString("o"))
                };

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        var authProperties = new AuthenticationProperties
        {
          IsPersistent = model.RememberMe,
          ExpiresUtc = response.TokenExpires,
          AllowRefresh = true
        };

        // Sign in user
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        // Store JWT token in a secure, httponly cookie
        Response.Cookies.Append("jwt_token", response.Token, new CookieOptions
        {
          HttpOnly = true,
          Secure = false,  // Di pengembangan gunakan false, di produksi gunakan true
          SameSite = SameSiteMode.Lax,
          Expires = response.TokenExpires
        });

        // Store refresh token in a separate secure cookie
        Response.Cookies.Append("refresh_token", response.RefreshToken, new CookieOptions
        {
          HttpOnly = true,
          Secure = false,  // Di pengembangan gunakan false, di produksi gunakan true
          SameSite = SameSiteMode.Lax,
          Expires = DateTime.Now.AddDays(7)
        });

        _logger.LogInformation("User {Username} logged in successfully", model.Username);

        // Security: Validate returnUrl is local
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
          return Redirect(returnUrl);
        }
        else
        {
          return RedirectToAction("Index", "Calendar");
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error during login for user {Username}", model.Username);
        ModelState.AddModelError(string.Empty, "Terjadi kesalahan saat login. Silakan coba lagi.");
        return View(model);
      }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
      try
      {
        var username = User.FindFirst("ldapuser")?.Value;

        // Log out from auth service
        if (!string.IsNullOrEmpty(username))
        {
          await _authService.LogoutAsync(username);
        }

        // Sign out user
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Remove cookies
        Response.Cookies.Delete("jwt_token");
        Response.Cookies.Delete("refresh_token");

        _logger.LogInformation("User {Username} logged out successfully", username);

        return RedirectToAction("Login", "Auth");
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error during logout");
        return RedirectToAction("Login", "Auth");
      }
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
      return View();
    }

    [HttpGet]
    public IActionResult RefreshToken()
    {
      // This is an Ajax endpoint for token refresh
      return Content("Token refresh functionality");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RefreshToken(string returnUrl = "/")
    {
      try
      {
        if (!Request.Cookies.TryGetValue("jwt_token", out var token) ||
            !Request.Cookies.TryGetValue("refresh_token", out var refreshToken))
        {
          // No tokens found, redirect to login
          return RedirectToAction("Login", "Auth", new { returnUrl });
        }

        var response = await _authService.RefreshTokenAsync(token, refreshToken);
        if (!response.Success)
        {
          // Token refresh failed, redirect to login
          return RedirectToAction("Login", "Auth", new { returnUrl });
        }

        // Update token cookies
        Response.Cookies.Append("jwt_token", response.Token, new CookieOptions
        {
          HttpOnly = true,
          Secure = false,  // Ubah ke false untuk pengembangan
          SameSite = SameSiteMode.Lax,  // Ubah ke Lax
          Expires = response.TokenExpires
        });

        // Store refresh token in a separate secure cookie
        Response.Cookies.Append("refresh_token", response.RefreshToken, new CookieOptions
        {
          HttpOnly = true,
          Secure = false,  // Ubah ke false untuk pengembangan
          SameSite = SameSiteMode.Lax,  // Ubah ke Lax
          Expires = DateTime.Now.AddDays(7)
        });

        // If it's AJAX request, return JSON
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
          return Json(new { success = true });
        }

        // Otherwise redirect
        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
          return Redirect(returnUrl);
        }
        else
        {
          return RedirectToAction("Index", "Calendar");
        }
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error refreshing token");

        // If it's AJAX request, return JSON
        if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
        {
          return Json(new { success = false, message = "Error refreshing token" });
        }

        return RedirectToAction("Login", "Auth", new { returnUrl });
      }
    }
  }
}
