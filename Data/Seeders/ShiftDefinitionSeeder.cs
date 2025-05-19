using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Models;
using System;

namespace AspnetCoreMvcFull.Data.Seeders
{
  public static class ShiftDefinitionSeeder
  {
    public static void Seed(ModelBuilder modelBuilder)
    {
      // Seed data untuk shift definitions
      modelBuilder.Entity<ShiftDefinition>().HasData(
          new ShiftDefinition
          {
            Id = 1,
            Name = "Shift 1",
            StartTime = new TimeSpan(7, 0, 0),  // 07:00 AM
            EndTime = new TimeSpan(15, 0, 0),   // 03:00 PM
            IsActive = true
          },
          new ShiftDefinition
          {
            Id = 2,
            Name = "Shift 2",
            StartTime = new TimeSpan(15, 0, 0), // 03:00 PM
            EndTime = new TimeSpan(23, 0, 0),   // 11:00 PM
            IsActive = true
          },
          new ShiftDefinition
          {
            Id = 3,
            Name = "Shift 3",
            StartTime = new TimeSpan(0, 0, 0), // 11:00 PM
            EndTime = new TimeSpan(7, 0, 0),    // 07:00 AM
            IsActive = true
          }
      );
    }
  }
}
