using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AspnetCoreMvcFull.Models.Auth;

namespace AspnetCoreMvcFull.Models.Role
{
  // Role constants - ditentukan secara tetap
  public static class Roles
  {
    public const string Admin = "admin";
    public const string PIC = "pic";
    public const string Operator = "operator";
    public const string MSD = "msd";
    public const string Manager = "manager"; // ✅ TAMBAHAN: Role manager

    public static readonly List<string> AllRoles = new List<string>
        {
            Admin, PIC, Operator, MSD, Manager
        };

    // ✅ TAMBAHAN: Role yang bisa di-assign manual
    public static readonly List<string> AssignableRoles = new List<string>
        {
            Admin, PIC, Operator, MSD
        };

    public static readonly Dictionary<string, string> RoleDescriptions = new Dictionary<string, string>
        {
            { Admin, "Administrator sistem, memiliki akses penuh ke semua fitur" },
            { PIC, "Penanggung Jawab Crane, bertanggung jawab atas pengelolaan pemesanan crane" },
            { Operator, "Operator Crane, bertugas untuk mengoperasikan crane" },
            { MSD, "Tim Maintenance, bertanggung jawab atas pemeliharaan crane" },
            { Manager, "Manager Departemen, otomatis berdasarkan position level di database karyawan" } // ✅ TAMBAHAN
        };
  }

  // Class untuk menyimpan assignment role ke user
  public class UserRole
  {
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string LdapUser { get; set; } = string.Empty;

    [Required]
    [StringLength(50)]
    public string RoleName { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [StringLength(100)]
    public string CreatedBy { get; set; } = string.Empty;

    public DateTime? UpdatedAt { get; set; }

    [StringLength(100)]
    public string? UpdatedBy { get; set; }

    [NotMapped]
    public EmployeeDetails? Employee { get; set; }
  }
}
