using System.ComponentModel.DataAnnotations;

namespace AspnetCoreMvcFull.ViewModels.Auth
{
  public class LoginViewModel
  {
    [Required(ErrorMessage = "Username tidak boleh kosong")]
    [Display(Name = "Username")]
    [StringLength(50, ErrorMessage = "Username tidak boleh lebih dari {1} karakter")]
    [RegularExpression(@"^[a-zA-Z0-9_.-]+$", ErrorMessage = "Username hanya boleh berisi huruf, angka, dan simbol .-_")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password tidak boleh kosong")]
    [Display(Name = "Password")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Ingat saya")]
    public bool RememberMe { get; set; }
  }
}
