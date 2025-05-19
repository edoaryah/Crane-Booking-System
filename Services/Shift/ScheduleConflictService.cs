// using Microsoft.EntityFrameworkCore;
// using AspnetCoreMvcFull.Data;

// namespace AspnetCoreMvcFull.Services
// {
//   public class ScheduleConflictService : IScheduleConflictService
//   {
//     private readonly AppDbContext _context;
//     private readonly ILogger<ScheduleConflictService> _logger;

//     public ScheduleConflictService(AppDbContext context, ILogger<ScheduleConflictService> logger)
//     {
//       _context = context;
//       _logger = logger;
//     }

//     // Ubah parameter menjadi int? untuk mendukung ShiftDefinitionId nullable
//     public async Task<bool> IsBookingConflictAsync(int craneId, DateTime date, int shiftDefinitionId, int? excludeBookingId = null)
//     {
//       try
//       {
//         // Gunakan tanggal lokal tanpa konversi
//         var dateLocal = date.Date;

//         var query = _context.BookingShifts
//             .Include(rs => rs.Booking)
//             .Where(rs => rs.Booking!.CraneId == craneId &&
//                     rs.Date.Date == dateLocal &&
//                     rs.ShiftDefinitionId == shiftDefinitionId); // EF Core menangani perbandingan int? == int

//         if (excludeBookingId.HasValue)
//         {
//           query = query.Where(rs => rs.BookingId != excludeBookingId.Value);
//         }

//         var existingBookings = await query.AnyAsync();
//         return existingBookings;
//       }
//       catch (Exception ex)
//       {
//         _logger.LogError(ex, "Error checking booking conflict for crane {CraneId}, date {Date}, shift {ShiftId}",
//             craneId, date, shiftDefinitionId);
//         throw;
//       }
//     }

//     // Ubah parameter menjadi int? untuk mendukung ShiftDefinitionId nullable
//     public async Task<bool> IsMaintenanceConflictAsync(int craneId, DateTime date, int shiftDefinitionId, int? excludeMaintenanceId = null)
//     {
//       try
//       {
//         // Gunakan tanggal lokal tanpa konversi
//         var dateLocal = date.Date;

//         var query = _context.MaintenanceScheduleShifts
//             .Include(ms => ms.MaintenanceSchedule)
//             .Where(ms => ms.MaintenanceSchedule!.CraneId == craneId &&
//                     ms.Date.Date == dateLocal &&
//                     ms.ShiftDefinitionId == shiftDefinitionId); // EF Core menangani perbandingan int? == int

//         if (excludeMaintenanceId.HasValue)
//         {
//           query = query.Where(ms => ms.MaintenanceScheduleId != excludeMaintenanceId.Value);
//         }

//         var existingMaintenance = await query.AnyAsync();
//         return existingMaintenance;
//       }
//       catch (Exception ex)
//       {
//         _logger.LogError(ex, "Error checking maintenance conflict for crane {CraneId}, date {Date}, shift {ShiftId}",
//             craneId, date, shiftDefinitionId);
//         throw;
//       }
//     }
//   }
// }

using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Data;

namespace AspnetCoreMvcFull.Services
{
  public class ScheduleConflictService : IScheduleConflictService
  {
    private readonly AppDbContext _context;
    private readonly ILogger<ScheduleConflictService> _logger;

    public ScheduleConflictService(AppDbContext context, ILogger<ScheduleConflictService> logger)
    {
      _context = context;
      _logger = logger;
    }

    // Metode original diperbarui untuk menggunakan pemeriksaan konflik berbasis waktu
    public async Task<bool> IsBookingConflictAsync(int craneId, DateTime date, int shiftDefinitionId, int? excludeBookingId = null)
    {
      try
      {
        // Dapatkan shift definition untuk mendapatkan rentang waktu
        var shiftDefinition = await _context.ShiftDefinitions.FindAsync(shiftDefinitionId);
        if (shiftDefinition == null)
        {
          throw new KeyNotFoundException($"Shift definition with ID {shiftDefinitionId} not found");
        }

        // Gunakan pemeriksaan konflik berbasis waktu
        return await IsBookingConflictByTimeAsync(craneId, date, shiftDefinition.StartTime, shiftDefinition.EndTime, excludeBookingId);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error checking booking conflict for crane {CraneId}, date {Date}, shift {ShiftId}",
            craneId, date, shiftDefinitionId);
        throw;
      }
    }

    public async Task<bool> IsMaintenanceConflictAsync(int craneId, DateTime date, int shiftDefinitionId, int? excludeMaintenanceId = null)
    {
      try
      {
        // Dapatkan shift definition untuk mendapatkan rentang waktu
        var shiftDefinition = await _context.ShiftDefinitions.FindAsync(shiftDefinitionId);
        if (shiftDefinition == null)
        {
          throw new KeyNotFoundException($"Shift definition with ID {shiftDefinitionId} not found");
        }

        // Gunakan pemeriksaan konflik berbasis waktu
        return await IsMaintenanceConflictByTimeAsync(craneId, date, shiftDefinition.StartTime, shiftDefinition.EndTime, excludeMaintenanceId);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error checking maintenance conflict for crane {CraneId}, date {Date}, shift {ShiftId}",
            craneId, date, shiftDefinitionId);
        throw;
      }
    }

    // Helper method untuk memeriksa apakah dua rentang waktu overlap
    private bool DoTimeRangesOverlap(DateTime start1, DateTime end1, DateTime start2, DateTime end2)
    {
      return start1 < end2 && end1 > start2;
    }

    // Helper method untuk mengkonversi tanggal dan rentang waktu ke waktu absolut
    private (DateTime Start, DateTime End) NormalizeTimeRange(DateTime date, TimeSpan startTime, TimeSpan endTime)
    {
      var start = date.Date.Add(startTime);
      var end = date.Date.Add(endTime);

      // Jika waktu akhir lebih kecil dari waktu mulai, shift melewati tengah malam
      if (endTime < startTime)
      {
        end = end.AddDays(1);
      }

      return (start, end);
    }

    // Metode baru untuk pemeriksaan konflik booking berbasis waktu
    public async Task<bool> IsBookingConflictByTimeAsync(int craneId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeBookingId = null)
    {
      try
      {
        // Normalisasi rentang waktu baru
        var (newStart, newEnd) = NormalizeTimeRange(date, startTime, endTime);

        // Query untuk booking shifts pada tanggal yang berpotensi konflik
        var datesToCheck = new[] { date.Date };
        if (endTime < startTime)
        {
          // Jika shift melewati tengah malam, cek juga hari berikutnya
          datesToCheck = new[] { date.Date, date.Date.AddDays(1) };
        }

        foreach (var dateToCheck in datesToCheck)
        {
          // Query untuk booking shifts pada tanggal ini
          var query = _context.BookingShifts
              .Include(rs => rs.Booking)
              .Where(rs => rs.Booking!.CraneId == craneId &&
                      rs.Date.Date == dateToCheck);

          if (excludeBookingId.HasValue)
          {
            query = query.Where(rs => rs.BookingId != excludeBookingId.Value);
          }

          // Dapatkan semua shifts untuk tanggal dan crane ini
          var existingShifts = await query.ToListAsync();

          // Periksa konflik dengan shift yang ada
          foreach (var shift in existingShifts)
          {
            // Normalisasi rentang waktu yang ada
            var (existingStart, existingEnd) = NormalizeTimeRange(shift.Date, shift.ShiftStartTime, shift.ShiftEndTime);

            // Periksa overlap
            if (DoTimeRangesOverlap(newStart, newEnd, existingStart, existingEnd))
            {
              return true;
            }
          }
        }

        // Tidak ditemukan konflik
        return false;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error checking booking conflict for crane {CraneId}, date {Date}, time range {StartTime}-{EndTime}",
            craneId, date, startTime, endTime);
        throw;
      }
    }

    // Metode baru untuk pemeriksaan konflik maintenance berbasis waktu
    public async Task<bool> IsMaintenanceConflictByTimeAsync(int craneId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeMaintenanceId = null)
    {
      try
      {
        // Normalisasi rentang waktu baru
        var (newStart, newEnd) = NormalizeTimeRange(date, startTime, endTime);

        // Query untuk maintenance shifts pada tanggal yang berpotensi konflik
        var datesToCheck = new[] { date.Date };
        if (endTime < startTime)
        {
          // Jika shift melewati tengah malam, cek juga hari berikutnya
          datesToCheck = new[] { date.Date, date.Date.AddDays(1) };
        }

        foreach (var dateToCheck in datesToCheck)
        {
          // Query untuk maintenance shifts pada tanggal ini
          var query = _context.MaintenanceScheduleShifts
              .Include(ms => ms.MaintenanceSchedule)
              .Where(ms => ms.MaintenanceSchedule!.CraneId == craneId &&
                      ms.Date.Date == dateToCheck);

          if (excludeMaintenanceId.HasValue)
          {
            query = query.Where(ms => ms.MaintenanceScheduleId != excludeMaintenanceId.Value);
          }

          // Dapatkan semua shifts untuk tanggal dan crane ini
          var existingShifts = await query.ToListAsync();

          // Periksa konflik dengan shift yang ada
          foreach (var shift in existingShifts)
          {
            // Normalisasi rentang waktu yang ada
            var (existingStart, existingEnd) = NormalizeTimeRange(shift.Date, shift.ShiftStartTime, shift.ShiftEndTime);

            // Periksa overlap
            if (DoTimeRangesOverlap(newStart, newEnd, existingStart, existingEnd))
            {
              return true;
            }
          }
        }

        // Tidak ditemukan konflik
        return false;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error checking maintenance conflict for crane {CraneId}, date {Date}, time range {StartTime}-{EndTime}",
            craneId, date, startTime, endTime);
        throw;
      }
    }
  }
}
