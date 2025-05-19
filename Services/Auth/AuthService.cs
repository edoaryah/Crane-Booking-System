// Services/Auth/AuthService.cs - kode yang dimodifikasi

using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using AspnetCoreMvcFull.Models.Auth;
using AspnetCoreMvcFull.Services.Common;
using AspnetCoreMvcFull.Services.Security;

namespace AspnetCoreMvcFull.Services.Auth
{
  public class AuthService : IAuthService
  {
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly string _sqlServerConnectionString;
    private readonly ICacheService _cacheService;
    private readonly ISecurityService _securityService;

    public AuthService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<AuthService> logger,
        ICacheService cacheService,
        ISecurityService securityService)
    {
      _httpClientFactory = httpClientFactory;
      _configuration = configuration;
      _logger = logger;
      _cacheService = cacheService;
      _securityService = securityService;

      _sqlServerConnectionString = configuration.GetConnectionString("SqlServerConnection")
          ?? throw new InvalidOperationException("SQL Server connection string 'SqlServerConnection' not found");
    }

    public async Task<AuthResponse> LoginAsync(string username, string password)
    {
      try
      {
        // Sanitize input
        username = _securityService.SanitizeInput(username);

        // For development environment, use mock login
        if (_configuration.GetValue<string>("AppSettings:Environment") == "Development")
        {
          var result = await MockLoginAsync(username, password);
          return result;
        }

        // For production environment, use company API
        string authUrl = _configuration["AuthApi:Url"]
            ?? "http://kpcsgt-appmembara.kpc.co.id:51051/login";

        var payload = new
        {
          username = username,
          password = password
        };

        var client = _httpClientFactory.CreateClient("AuthClient");
        client.Timeout = TimeSpan.FromSeconds(30); // Set reasonable timeout

        var response = await client.PostAsJsonAsync(authUrl, payload);

        if (response.IsSuccessStatusCode)
        {
          var data = await response.Content.ReadAsStringAsync();
          var authResponse = JsonConvert.DeserializeObject<AuthResponse>(data);

          if (authResponse != null && authResponse.Success)
          {
            _logger.LogInformation("User {Username} successfully authenticated", username);

            // Enhance with refresh token if the API doesn't provide one
            if (string.IsNullOrEmpty(authResponse.RefreshToken))
            {
              authResponse.RefreshToken = _securityService.GenerateRefreshToken();
              authResponse.TokenExpires = DateTime.Now.AddDays(7);

              // Store refresh token in cache or database
              await _cacheService.SetAsync($"refresh_token:{username}", authResponse.RefreshToken, TimeSpan.FromDays(7));
            }

            return authResponse;
          }

          _logger.LogWarning("Authentication failed for user {Username} with message: {Message}",
              username, authResponse?.Message ?? "Unknown error");

          return authResponse ?? new AuthResponse { Success = false, Message = "Gagal memproses respons autentikasi" };
        }

        _logger.LogWarning("Authentication failed for user {Username}. Status code: {StatusCode}",
            username, response.StatusCode);

        return new AuthResponse
        {
          Success = false,
          Message = $"Autentikasi gagal. Periksa kembali username dan password Anda."
        };
      }
      catch (HttpRequestException ex)
      {
        _logger.LogError(ex, "Network error during login for user {Username}", username);
        return new AuthResponse
        {
          Success = false,
          Message = "Tidak dapat terhubung ke server autentikasi. Silakan coba lagi nanti."
        };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error during login for user {Username}", username);
        return new AuthResponse
        {
          Success = false,
          Message = "Terjadi kesalahan saat proses autentikasi. Silakan coba lagi."
        };
      }
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
      try
      {
        if (string.IsNullOrEmpty(token))
        {
          return false;
        }

        // For development, implement simple token validation
        if (_configuration.GetValue<string>("AppSettings:Environment") == "Development")
        {
          // Simple check to see if token is in correct format
          return !string.IsNullOrEmpty(token) && token.Length > 20;
        }

        // For production, validate token against company API or using JWT validation
        string validateUrl = _configuration["AuthApi:ValidateUrl"]
            ?? "http://kpcsgt-appmembara.kpc.co.id:51051/validate";

        var client = _httpClientFactory.CreateClient("AuthClient");
        var response = await client.PostAsJsonAsync(validateUrl, new { token });

        return response.IsSuccessStatusCode;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error validating token");
        return false;
      }
    }

    public async Task<AuthResponse> RefreshTokenAsync(string token, string refreshToken)
    {
      try
      {
        // For development, implement simple token refresh
        if (_configuration.GetValue<string>("AppSettings:Environment") == "Development")
        {
          return new AuthResponse
          {
            Success = true,
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            RefreshToken = refreshToken, // Keep same refresh token
            TokenExpires = DateTime.Now.AddHours(8),
            Message = "Token refreshed successfully"
          };
        }

        // For production, refresh token against company API
        string refreshUrl = _configuration["AuthApi:RefreshUrl"]
            ?? "http://kpcsgt-appmembara.kpc.co.id:51051/refresh";

        var client = _httpClientFactory.CreateClient("AuthClient");
        var response = await client.PostAsJsonAsync(refreshUrl, new { token, refreshToken });

        if (response.IsSuccessStatusCode)
        {
          var data = await response.Content.ReadAsStringAsync();
          var authResponse = JsonConvert.DeserializeObject<AuthResponse>(data);

          return authResponse ?? new AuthResponse
          {
            Success = false,
            Message = "Gagal memperbaharui token"
          };
        }

        return new AuthResponse
        {
          Success = false,
          Message = "Token tidak valid atau telah kedaluwarsa. Silakan login kembali."
        };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error refreshing token");
        return new AuthResponse
        {
          Success = false,
          Message = "Terjadi kesalahan saat memperbarui token. Silakan login kembali."
        };
      }
    }

    public async Task<bool> LogoutAsync(string username)
    {
      try
      {
        // Clear cache entries
        await _cacheService.RemoveAsync($"refresh_token:{username}");

        // For development, simple logout is enough
        if (_configuration.GetValue<string>("AppSettings:Environment") == "Development")
        {
          return true;
        }

        // For production, you might need to invalidate token on the server
        string? logoutUrl = _configuration["AuthApi:LogoutUrl"];
        if (!string.IsNullOrEmpty(logoutUrl))
        {
          var client = _httpClientFactory.CreateClient("AuthClient");
          var response = await client.PostAsJsonAsync(logoutUrl, new { username });
          return response.IsSuccessStatusCode;
        }

        return true;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error during logout for user {Username}", username);
        return false;
      }
    }

    private async Task<AuthResponse> MockLoginAsync(string username, string password)
    {
      // Validate simple mock for development
      if (username != password)
      {
        _logger.LogWarning("Mock login failed: Invalid credentials for user {Username}", username);
        return new AuthResponse
        {
          Success = false,
          Message = "Username atau password tidak valid"
        };
      }

      // Retrieve employee data from database
      var employee = await GetEmployeeByLdapUserAsync(username);
      if (employee == null)
      {
        _logger.LogWarning("Mock login failed: User {Username} not found in database", username);
        return new AuthResponse
        {
          Success = false,
          Message = "Username atau password tidak valid"
        };
      }

      // Generate mock token and refresh token
      string mockToken = _securityService.GenerateMockJwtToken(employee);
      string refreshToken = _securityService.GenerateRefreshToken();

      // Store refresh token
      await _cacheService.SetAsync($"refresh_token:{username}", refreshToken, TimeSpan.FromDays(7));

      _logger.LogInformation("User {Username} mock login successful", username);
      return new AuthResponse
      {
        Success = true,
        Message = "Login berhasil",
        Token = mockToken,
        RefreshToken = refreshToken,
        TokenExpires = DateTime.Now.AddHours(8),
        Employee = employee
      };
    }

    public async Task<EmployeeDetails?> GetEmployeeByLdapUserAsync(string ldapUser)
    {
      if (string.IsNullOrEmpty(ldapUser))
      {
        throw new ArgumentException("LDAP username cannot be null or empty", nameof(ldapUser));
      }

      // Check cache first
      var cachedEmployee = await _cacheService.GetAsync<EmployeeDetails>($"employee:{ldapUser}");
      if (cachedEmployee != null)
      {
        return cachedEmployee;
      }

      EmployeeDetails? employee = null;

      try
      {
        using (SqlConnection connection = new SqlConnection(_sqlServerConnectionString))
        {
          await connection.OpenAsync();

          string query = "SELECT * FROM SP_EMPLIST WHERE LDAPUSER = @LdapUser";
          using (SqlCommand command = new SqlCommand(query, connection))
          {
            command.Parameters.AddWithValue("@LdapUser", ldapUser);
            command.CommandTimeout = 30; // Set reasonable timeout

            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
              if (await reader.ReadAsync())
              {
                employee = new EmployeeDetails
                {
                  EmpId = reader["EMP_ID"]?.ToString() ?? string.Empty,
                  Name = reader["NAME"]?.ToString() ?? string.Empty,
                  PositionTitle = reader["POSITION_TITLE"]?.ToString() ?? string.Empty,
                  Division = reader["DIVISION"]?.ToString() ?? string.Empty,
                  Department = reader["DEPARTMENT"]?.ToString() ?? string.Empty,
                  Email = reader["EMAIL"]?.ToString() ?? string.Empty,
                  PositionLvl = reader["POSITION_LVL"]?.ToString() ?? string.Empty,
                  LdapUser = reader["LDAPUSER"]?.ToString() ?? string.Empty,
                  EmpStatus = reader["EMP_STATUS"]?.ToString() ?? string.Empty
                };
              }
            }
          }
        }

        // Cache employee data if found
        if (employee != null)
        {
          await _cacheService.SetAsync($"employee:{ldapUser}", employee, TimeSpan.FromHours(1));
        }
      }
      catch (SqlException ex)
      {
        _logger.LogError(ex, "Database error fetching employee data for LDAP user {LdapUser}", ldapUser);
        throw;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error fetching employee data for LDAP user {LdapUser}", ldapUser);
        throw;
      }

      return employee;
    }
  }
}
