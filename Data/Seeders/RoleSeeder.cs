using AspnetCoreMvcFull.Models.Role;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Data.Seeders
{
  public static class RoleSeeder
  {
    public static void Seed(ModelBuilder modelBuilder)
    {
      // Tambahkan seeder untuk user role admin
      // Ganti "admin_username" dengan username LDAP yang ingin Anda jadikan admin default
      modelBuilder.Entity<UserRole>().HasData(
          new UserRole
          {
            Id = 1,
            LdapUser = "PIC1", // Ganti dengan username LDAP Anda
            RoleName = Roles.Admin,
            Notes = "Default admin user created by seeder",
            CreatedAt = new DateTime(2023, 1, 1), // Nilai statis
            CreatedBy = "system"
          },
          new UserRole
          {
            Id = 2,
            LdapUser = "PIC1", // Ganti dengan username LDAP Anda
            RoleName = Roles.PIC,
            Notes = "Default admin user created by seeder",
            CreatedAt = new DateTime(2023, 1, 1), // Nilai statis
            CreatedBy = "system"
          }
      );
    }
  }
}
