// Data/AppDbContext.cs
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Models.Auth;
using AspnetCoreMvcFull.Models.Role;
using AspnetCoreMvcFull.Data.Seeders;
using AspnetCoreMvcFull.Models;

namespace AspnetCoreMvcFull.Data
{
  public class AppDbContext : DbContext
  {
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // crane
    public DbSet<Crane> Cranes { get; set; }

    // shift
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<BookingHazard> BookingHazards { get; set; }
    public DbSet<BookingItem> BookingItems { get; set; }
    public DbSet<BookingShift> BookingShifts { get; set; }

    // hazard & shift
    public DbSet<Hazard> Hazards { get; set; }
    public DbSet<ShiftDefinition> ShiftDefinitions { get; set; }

    // breakdown & maintenance
    public DbSet<Breakdown> Breakdowns { get; set; }
    public DbSet<MaintenanceSchedule> MaintenanceSchedules { get; set; }
    public DbSet<MaintenanceScheduleShift> MaintenanceScheduleShifts { get; set; }

    // auth
    public DbSet<UserRole> UserRoles { get; set; }

    // crane usage records
    public DbSet<UsageSubcategory> UsageSubcategories { get; set; }
    public DbSet<CraneUsageRecord> CraneUsageRecords { get; set; }
    public DbSet<CraneUsageEntry> CraneUsageEntries { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      // Configure Enum conversions for CraneStatus
      modelBuilder.Entity<Crane>()
          .Property(c => c.Status)
          .HasConversion<string>();

      // Configure Enum conversions for CraneOwnership
      modelBuilder.Entity<Crane>()
          .Property(c => c.Ownership)
          .HasConversion<string>();

      // Configure Enum conversions for UsageCategory
      modelBuilder.Entity<UsageSubcategory>()
          .Property(s => s.Category)
          .HasConversion<string>();

      modelBuilder.Entity<CraneUsageEntry>()
          .Property(e => e.Category)
          .HasConversion<string>();

      // Relasi Crane dan Breakdown
      modelBuilder.Entity<Breakdown>()
          .HasOne(u => u.Crane)
          .WithMany(c => c.Breakdowns)
          .HasForeignKey(u => u.CraneId)
          .OnDelete(DeleteBehavior.Cascade);

      // Relasi Crane dan Booking
      modelBuilder.Entity<Booking>()
          .HasOne(r => r.Crane)
          .WithMany()
          .HasForeignKey(r => r.CraneId)
          .OnDelete(DeleteBehavior.Cascade);

      // Relasi Booking dan BookingShift
      modelBuilder.Entity<BookingShift>()
          .HasOne(rs => rs.Booking)
          .WithMany(r => r.BookingShifts)
          .HasForeignKey(rs => rs.BookingId)
          .OnDelete(DeleteBehavior.Cascade);

      modelBuilder.Entity<BookingShift>()
          .HasOne(bs => bs.ShiftDefinition)
          .WithMany(sd => sd.BookingShifts)
          .HasForeignKey(bs => bs.ShiftDefinitionId)
          .OnDelete(DeleteBehavior.SetNull); // Ubah dari Restrict menjadi SetNull

      // Relasi Booking dan BookingItem
      modelBuilder.Entity<BookingItem>()
          .HasOne(bi => bi.Booking)
          .WithMany(b => b.BookingItems)
          .HasForeignKey(bi => bi.BookingId)
          .OnDelete(DeleteBehavior.Cascade);

      // Relasi BookingHazard
      modelBuilder.Entity<BookingHazard>()
          .HasOne(bh => bh.Booking)
          .WithMany(b => b.BookingHazards)
          .HasForeignKey(bh => bh.BookingId)
          .OnDelete(DeleteBehavior.Cascade);

      modelBuilder.Entity<BookingHazard>()
          .HasOne(bh => bh.Hazard)
          .WithMany()
          .HasForeignKey(bh => bh.HazardId)
          .OnDelete(DeleteBehavior.SetNull); // Ubah dari Restrict menjadi SetNull

      // Relasi MaintenanceSchedule dan Crane
      modelBuilder.Entity<MaintenanceSchedule>()
          .HasOne(ms => ms.Crane)
          .WithMany()
          .HasForeignKey(ms => ms.CraneId)
          .OnDelete(DeleteBehavior.Cascade);

      // Relasi MaintenanceSchedule dan MaintenanceScheduleShift
      modelBuilder.Entity<MaintenanceScheduleShift>()
          .HasOne(mss => mss.MaintenanceSchedule)
          .WithMany(ms => ms.MaintenanceScheduleShifts)
          .HasForeignKey(mss => mss.MaintenanceScheduleId)
          .OnDelete(DeleteBehavior.Cascade);

      // Relasi MaintenanceScheduleShift dan ShiftDefinition
      modelBuilder.Entity<MaintenanceScheduleShift>()
          .HasOne(mss => mss.ShiftDefinition)
          .WithMany()
          .HasForeignKey(mss => mss.ShiftDefinitionId)
          .OnDelete(DeleteBehavior.SetNull); // Ubah dari Restrict menjadi SetNull

      // Configure UserRole entity
      modelBuilder.Entity<UserRole>(entity =>
      {
        entity.ToTable("UserRoles");
        entity.HasKey(e => e.Id);
        entity.Property(e => e.LdapUser).IsRequired().HasMaxLength(50);
        entity.Property(e => e.RoleName).IsRequired().HasMaxLength(50);
        entity.Property(e => e.Notes).HasMaxLength(255);
        entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
        entity.Property(e => e.UpdatedBy).HasMaxLength(100);

        // Create a unique index on LdapUser + RoleName
        entity.HasIndex(e => new { e.LdapUser, e.RoleName }).IsUnique();
      });

      // Relasi CraneUsageRecord dan Crane
      modelBuilder.Entity<CraneUsageRecord>()
          .HasOne(r => r.Crane)
          .WithMany()
          .HasForeignKey(r => r.CraneId)
          .OnDelete(DeleteBehavior.Cascade);

      // Relasi CraneUsageEntry dan CraneUsageRecord
      modelBuilder.Entity<CraneUsageEntry>()
          .HasOne(e => e.CraneUsageRecord)
          .WithMany(r => r.Entries)
          .HasForeignKey(e => e.CraneUsageRecordId)
          .OnDelete(DeleteBehavior.Cascade);

      modelBuilder.Entity<CraneUsageEntry>()
          .HasOne(e => e.UsageSubcategory)
          .WithMany()
          .HasForeignKey(e => e.UsageSubcategoryId)
          .OnDelete(DeleteBehavior.SetNull); // Diubah dari Restrict menjadi SetNull

      // Relasi CraneUsageEntry dan Booking
      modelBuilder.Entity<CraneUsageEntry>()
          .HasOne(e => e.Booking)
          .WithMany()
          .HasForeignKey(e => e.BookingId)
          .OnDelete(DeleteBehavior.SetNull);

      // Relasi CraneUsageEntry dan MaintenanceSchedule
      modelBuilder.Entity<CraneUsageEntry>()
          .HasOne(e => e.MaintenanceSchedule)
          .WithMany()
          .HasForeignKey(e => e.MaintenanceScheduleId)
          .OnDelete(DeleteBehavior.SetNull);

      CraneSeeder.Seed(modelBuilder);
      RoleSeeder.Seed(modelBuilder);
      HazardSeeder.Seed(modelBuilder);
      ShiftDefinitionSeeder.Seed(modelBuilder);
      UsageSubcategorySeeder.Seed(modelBuilder);
    }
  }
}
