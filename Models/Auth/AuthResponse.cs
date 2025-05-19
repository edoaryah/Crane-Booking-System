namespace AspnetCoreMvcFull.Models.Auth
{
  public class AuthResponse
  {
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime TokenExpires { get; set; }
    public EmployeeDetails? Employee { get; set; }
  }
}
