using AspnetCoreMvcFull.Models.Auth;

namespace AspnetCoreMvcFull.Services.Auth
{
  public interface IAuthService
  {
    Task<AuthResponse> LoginAsync(string username, string password);
    Task<bool> ValidateTokenAsync(string token);
    Task<AuthResponse> RefreshTokenAsync(string token, string refreshToken);
    Task<EmployeeDetails?> GetEmployeeByLdapUserAsync(string ldapUser);
    Task<bool> LogoutAsync(string username);
    // Task<bool> IsAccountLockedAsync(string username);
    // Task RecordLoginAttemptAsync(string username, bool successful);
  }
}
