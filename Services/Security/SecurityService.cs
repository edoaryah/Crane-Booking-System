using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.IdentityModel.Tokens;
using AspnetCoreMvcFull.Models.Auth;

namespace AspnetCoreMvcFull.Services.Security
{
  public class SecurityService : ISecurityService
  {
    private readonly IConfiguration _configuration;
    private readonly ILogger<SecurityService> _logger;

    public SecurityService(IConfiguration configuration, ILogger<SecurityService> logger)
    {
      _configuration = configuration;
      _logger = logger;
    }

    public string SanitizeInput(string input)
    {
      if (string.IsNullOrEmpty(input))
      {
        return string.Empty;
      }

      // Remove potentially dangerous characters
      input = Regex.Replace(input, @"[^\w\d\s\-\._@]", "");

      // Trim and limit length
      return input.Trim().Substring(0, Math.Min(input.Length, 100));
    }

    public string GenerateRefreshToken()
    {
      var randomNumber = new byte[32];
      using var rng = RandomNumberGenerator.Create();
      rng.GetBytes(randomNumber);
      return Convert.ToBase64String(randomNumber);
    }

    public string GenerateMockJwtToken(EmployeeDetails employee)
    {
      try
      {
        var securityKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "DefaultDevSecretKey12345678901234567890"));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
                    new Claim(JwtRegisteredClaimNames.Sub, employee.EmpId),
                    new Claim(JwtRegisteredClaimNames.Name, employee.Name),
                    new Claim("ldapuser", employee.LdapUser),
                    new Claim("department", employee.Department),
                    new Claim("division", employee.Division),
                    new Claim("position", employee.PositionTitle),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "DevelopmentIssuer",
            audience: _configuration["Jwt:Audience"] ?? "DevelopmentAudience",
            claims: claims,
            expires: DateTime.Now.AddHours(8),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error generating mock JWT token for employee {EmployeeId}", employee.EmpId);
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray()); // Fallback to simple token
      }
    }

    public bool ValidateMockJwtToken(string token, out string? username)
    {
      username = null;

      try
      {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "DefaultDevSecretKey12345678901234567890");

        tokenHandler.ValidateToken(token, new TokenValidationParameters
        {
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(key),
          ValidateIssuer = true,
          ValidIssuer = _configuration["Jwt:Issuer"] ?? "DevelopmentIssuer",
          ValidateAudience = true,
          ValidAudience = _configuration["Jwt:Audience"] ?? "DevelopmentAudience",
          ValidateLifetime = true,
          ClockSkew = TimeSpan.Zero
        }, out SecurityToken validatedToken);

        var jwtToken = (JwtSecurityToken)validatedToken;
        username = jwtToken.Claims.FirstOrDefault(x => x.Type == "ldapuser")?.Value;

        return true;
      }
      catch (Exception ex)
      {
        _logger.LogWarning(ex, "JWT token validation failed");
        return false;
      }
    }
  }
}
