using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.Data.Seeders
{
  public static class CraneSeeder
  {
    public static void Seed(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<Crane>().HasData(
          new Crane { Id = 1, Code = "LC008", Capacity = 250, Ownership = CraneOwnership.KPC },
          new Crane { Id = 2, Code = "LC009", Capacity = 150, Ownership = CraneOwnership.KPC },
          new Crane { Id = 3, Code = "LC010", Capacity = 100, Ownership = CraneOwnership.KPC },
          new Crane { Id = 4, Code = "LC011", Capacity = 150, Ownership = CraneOwnership.KPC },
          new Crane { Id = 5, Code = "LC012", Capacity = 35, Ownership = CraneOwnership.KPC },
          new Crane { Id = 6, Code = "LC013", Capacity = 15, Ownership = CraneOwnership.KPC }
      );
    }
  }
}
