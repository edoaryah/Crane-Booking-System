using Microsoft.AspNetCore.Authentication;
using AspnetCoreMvcFull.Services.Auth;

namespace AspnetCoreMvcFull.Middleware
{
  public class TokenRefreshMiddleware
  {
    private readonly RequestDelegate _next;
    private readonly ILogger<TokenRefreshMiddleware> _logger;

    public TokenRefreshMiddleware(RequestDelegate next, ILogger<TokenRefreshMiddleware> logger)
    {
      _next = next;
      _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuthService authService)
    {
      try
      {
        // Check if user is authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
          // Get authentication time
          var authTimeClaim = context.User.FindFirst("auth_time");
          if (authTimeClaim != null && DateTime.TryParse(authTimeClaim.Value, out var authTime))
          {
            // If authentication is older than 6 hours, try to refresh
            if (DateTime.Now.Subtract(authTime) > TimeSpan.FromHours(6))
            {
              if (context.Request.Cookies.TryGetValue("jwt_token", out var token) &&
                  context.Request.Cookies.TryGetValue("refresh_token", out var refreshToken))
              {
                var response = await authService.RefreshTokenAsync(token, refreshToken);
                if (response.Success)
                {
                  // Update token cookies
                  context.Response.Cookies.Append("jwt_token", response.Token, new CookieOptions
                  {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = response.TokenExpires
                  });

                  // Store refresh token
                  context.Response.Cookies.Append("refresh_token", response.RefreshToken, new CookieOptions
                  {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.Now.AddDays(7)
                  });

                  // Update user claims
                  var username = context.User.FindFirst("ldapuser")?.Value;
                  if (!string.IsNullOrEmpty(username))
                  {
                    _logger.LogInformation("Token refreshed for user {Username}", username);
                  }
                }
                else
                {
                  // Token refresh failed, sign out user
                  await context.SignOutAsync();
                  context.Response.Cookies.Delete("jwt_token");
                  context.Response.Cookies.Delete("refresh_token");

                  // Redirect to login
                  context.Response.Redirect("/Auth/Login");
                  return;
                }
              }
            }
          }
        }

        await _next(context);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error in token refresh middleware");
        await _next(context);
      }
    }
  }

  // Extension method for using the middleware
  public static class TokenRefreshMiddlewareExtensions
  {
    public static IApplicationBuilder UseTokenRefresh(this IApplicationBuilder builder)
    {
      return builder.UseMiddleware<TokenRefreshMiddleware>();
    }
  }
}
