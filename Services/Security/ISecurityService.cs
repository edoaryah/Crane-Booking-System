using AspnetCoreMvcFull.Models.Auth;

namespace AspnetCoreMvcFull.Services.Security
{
  public interface ISecurityService
  {
    string SanitizeInput(string input);
    string GenerateRefreshToken();
    string GenerateMockJwtToken(EmployeeDetails employee);
    bool ValidateMockJwtToken(string token, out string? username);
  }
}
