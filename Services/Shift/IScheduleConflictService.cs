// namespace AspnetCoreMvcFull.Services
// {
//   public interface IScheduleConflictService
//   {
//     // Ubah parameter menjadi int? untuk konsistensi dengan implementasi
//     Task<bool> IsBookingConflictAsync(int craneId, DateTime date, int shiftDefinitionId, int? excludeBookingId = null);
//     Task<bool> IsMaintenanceConflictAsync(int craneId, DateTime date, int shiftDefinitionId, int? excludeMaintenanceId = null);
//   }
// }

// namespace AspnetCoreMvcFull.Services
// {
//   public interface IScheduleConflictService
//   {
//     // Metode original (untuk backward compatibility)
//     Task<bool> IsBookingConflictAsync(int craneId, DateTime date, int shiftDefinitionId, int? excludeBookingId = null);
//     Task<bool> IsMaintenanceConflictAsync(int craneId, DateTime date, int shiftDefinitionId, int? excludeMaintenanceId = null);

//     // Metode baru berbasis rentang waktu
//     Task<bool> IsBookingConflictByTimeAsync(int craneId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeBookingId = null);
//     Task<bool> IsMaintenanceConflictByTimeAsync(int craneId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeMaintenanceId = null);
//   }
// }

public interface IScheduleConflictService
{
  // Metode original
  // Task<bool> IsBookingConflictAsync(int craneId, DateTime date, int shiftDefinitionId, int? excludeBookingId = null);
  Task<bool> IsBookingConflictAsync(int craneId, DateTime date, int shiftDefinitionId, int? excludeBookingId = null, string craneCode = null);

  Task<bool> IsMaintenanceConflictAsync(int craneId, DateTime date, int shiftDefinitionId, int? excludeMaintenanceId = null, string craneCode = null);

  // Metode berbasis waktu
  Task<bool> IsBookingConflictByTimeAsync(int craneId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeBookingId = null, string craneCode = null);
  Task<bool> IsMaintenanceConflictByTimeAsync(int craneId, DateTime date, TimeSpan startTime, TimeSpan endTime, int? excludeMaintenanceId = null, string craneCode = null);
}
