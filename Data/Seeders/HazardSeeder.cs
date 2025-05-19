using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.Data.Seeders
{
  public static class HazardSeeder
  {
    public static void Seed(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Hazard>().HasData(
          new Hazard { Id = 1, Name = "Listrik Tegangan Tinggi" },
          new Hazard { Id = 2, Name = "Kondisi Tanah" },
          new Hazard { Id = 3, Name = "Bekerja di Dekat Bangunan" },
          new Hazard { Id = 4, Name = "Bekerja di Dekat Area Mining" },
          new Hazard { Id = 5, Name = "Bekerja di Dekat Air" }
      );
    }
  }
}
