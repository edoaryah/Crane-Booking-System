using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.Data.Seeders
{
  public static class UsageSubcategorySeeder
  {
    public static void Seed(ModelBuilder modelBuilder)
    {
      // Operating subcategories
      modelBuilder.Entity<UsageSubcategory>().HasData(
          new UsageSubcategory { Id = 1, Category = UsageCategory.Operating, Name = "Pengangkatan", Description = "Crane used for production lifting operations", IsActive = true },
          new UsageSubcategory { Id = 2, Category = UsageCategory.Operating, Name = "Menggantung Beban", Description = "Crane used for equipment installation", IsActive = true }
      );

      // Delay subcategories
      modelBuilder.Entity<UsageSubcategory>().HasData(
          new UsageSubcategory { Id = 3, Category = UsageCategory.Delay, Name = "Traveling", Description = "Delay due to bad weather conditions", IsActive = true },
          new UsageSubcategory { Id = 4, Category = UsageCategory.Delay, Name = "Prestart Check", Description = "Delay due to bad weather conditions", IsActive = true },
          new UsageSubcategory { Id = 5, Category = UsageCategory.Delay, Name = "Menunggu User", Description = "Delay due to planning or coordination issues", IsActive = true },
          new UsageSubcategory { Id = 6, Category = UsageCategory.Delay, Name = "Menunggu Kesiapan Pengangkatan", Description = "Scheduled operator break time", IsActive = true },
          new UsageSubcategory { Id = 7, Category = UsageCategory.Delay, Name = "Menunggu Pengawalan", Description = "Delay for refueling operations", IsActive = true },
          new UsageSubcategory { Id = 20, Category = UsageCategory.Delay, Name = "Fueling", Description = "Delay for refueling operations", IsActive = true },
          new UsageSubcategory { Id = 21, Category = UsageCategory.Delay, Name = "Cuaca", Description = "Delay for refueling operations", IsActive = true }
      );

      // Standby subcategories
      modelBuilder.Entity<UsageSubcategory>().HasData(
          new UsageSubcategory { Id = 8, Category = UsageCategory.Standby, Name = "Tidak ada Operator", Description = "Crane on standby at work site", IsActive = true },
          new UsageSubcategory { Id = 9, Category = UsageCategory.Standby, Name = "Tidak diperlukan", Description = "Crane available but not assigned any tasks", IsActive = true },
          new UsageSubcategory { Id = 10, Category = UsageCategory.Standby, Name = "Tidak ada pengawal", Description = "Planned standby period", IsActive = true },
          new UsageSubcategory { Id = 22, Category = UsageCategory.Standby, Name = "Istirahat", Description = "Planned standby period", IsActive = true },
          new UsageSubcategory { Id = 23, Category = UsageCategory.Standby, Name = "Ganti Shift", Description = "Planned standby period", IsActive = true },
          new UsageSubcategory { Id = 24, Category = UsageCategory.Standby, Name = "Tidak Bisa Lewat", Description = "Planned standby period", IsActive = true }
      );

      // Service subcategories
      modelBuilder.Entity<UsageSubcategory>().HasData(
          new UsageSubcategory { Id = 11, Category = UsageCategory.Service, Name = "Servis Rutin Terjadwal", Description = "Regular scheduled maintenance", IsActive = true }
      );

      // Breakdown subcategories
      modelBuilder.Entity<UsageSubcategory>().HasData(
          new UsageSubcategory { Id = 15, Category = UsageCategory.Breakdown, Name = "Rusak", Description = "Breakdown due to mechanical problems", IsActive = true },
          new UsageSubcategory { Id = 16, Category = UsageCategory.Breakdown, Name = "Perbaikan", Description = "Breakdown due to electrical problems", IsActive = true }
      );
    }
  }
}
