using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.ViewModels.ShiftManagement;

namespace AspnetCoreMvcFull.Services
{
  public class ShiftDefinitionService : IShiftDefinitionService
  {
    private readonly AppDbContext _context;
    private readonly ILogger<ShiftDefinitionService> _logger;

    public ShiftDefinitionService(AppDbContext context, ILogger<ShiftDefinitionService> logger)
    {
      _context = context;
      _logger = logger;
    }

    public async Task<IEnumerable<ShiftViewModel>> GetAllShiftDefinitionsAsync()
    {
      var shifts = await _context.ShiftDefinitions
          .OrderBy(s => s.StartTime)
          .ToListAsync();

      return shifts.Select(s => new ShiftViewModel
      {
        Id = s.Id,
        Name = s.Name,
        StartTime = s.StartTime,
        EndTime = s.EndTime,
        IsActive = s.IsActive
      }).ToList();
    }

    public async Task<ShiftViewModel> GetShiftDefinitionByIdAsync(int id)
    {
      var shift = await _context.ShiftDefinitions.FindAsync(id);

      if (shift == null)
      {
        throw new KeyNotFoundException($"Shift definition with ID {id} not found");
      }

      return new ShiftViewModel
      {
        Id = shift.Id,
        Name = shift.Name,
        StartTime = shift.StartTime,
        EndTime = shift.EndTime,
        IsActive = shift.IsActive
      };
    }

    public async Task<ShiftViewModel> CreateShiftDefinitionAsync(ShiftCreateViewModel shiftViewModel)
    {
      // Validate time range
      if (shiftViewModel.StartTime >= shiftViewModel.EndTime)
      {
        throw new ArgumentException("Start time must be before end time");
      }

      // Check for overlapping shifts
      var overlappingShifts = await _context.ShiftDefinitions
          .Where(s => shiftViewModel.StartTime < s.EndTime && shiftViewModel.EndTime > s.StartTime)
          .ToListAsync();

      if (overlappingShifts.Any())
      {
        throw new InvalidOperationException($"The new shift overlaps with existing shifts: {string.Join(", ", overlappingShifts.Select(s => s.Name))}");
      }

      var shift = new ShiftDefinition
      {
        Name = shiftViewModel.Name,
        StartTime = shiftViewModel.StartTime,
        EndTime = shiftViewModel.EndTime,
        IsActive = shiftViewModel.IsActive
      };

      _context.ShiftDefinitions.Add(shift);
      await _context.SaveChangesAsync();

      return new ShiftViewModel
      {
        Id = shift.Id,
        Name = shift.Name,
        StartTime = shift.StartTime,
        EndTime = shift.EndTime,
        IsActive = shift.IsActive
      };
    }

    public async Task<ShiftViewModel> UpdateShiftDefinitionAsync(int id, ShiftUpdateViewModel shiftViewModel)
    {
      var shift = await _context.ShiftDefinitions.FindAsync(id);

      if (shift == null)
      {
        throw new KeyNotFoundException($"Shift definition with ID {id} not found");
      }

      // Validate time range
      if (shiftViewModel.StartTime >= shiftViewModel.EndTime)
      {
        throw new ArgumentException("Start time must be before end time");
      }

      // Check for overlapping shifts (excluding the current one)
      var overlappingShifts = await _context.ShiftDefinitions
          .Where(s => s.Id != id &&
                 (shiftViewModel.StartTime < s.EndTime && shiftViewModel.EndTime > s.StartTime))
          .ToListAsync();

      if (overlappingShifts.Any())
      {
        throw new InvalidOperationException($"The updated shift overlaps with existing shifts: {string.Join(", ", overlappingShifts.Select(s => s.Name))}");
      }

      // Update shift properties
      shift.Name = shiftViewModel.Name;
      shift.StartTime = shiftViewModel.StartTime;
      shift.EndTime = shiftViewModel.EndTime;
      shift.IsActive = shiftViewModel.IsActive;

      await _context.SaveChangesAsync();

      return new ShiftViewModel
      {
        Id = shift.Id,
        Name = shift.Name,
        StartTime = shift.StartTime,
        EndTime = shift.EndTime,
        IsActive = shift.IsActive
      };
    }

    public async Task DeleteShiftDefinitionAsync(int id)
    {
      var shift = await _context.ShiftDefinitions
          .Include(s => s.BookingShifts)
          .FirstOrDefaultAsync(s => s.Id == id);

      if (shift == null)
      {
        throw new KeyNotFoundException($"Shift definition with ID {id} not found");
      }

      // Periksa apakah shift digunakan oleh BookingShift
      if (shift.BookingShifts.Any())
      {
        // Update semua BookingShift yang menggunakan shift ini
        foreach (var bookingShift in shift.BookingShifts)
        {
          // Pastikan data historis terisi
          bookingShift.ShiftName = shift.Name;
          bookingShift.ShiftStartTime = shift.StartTime;
          bookingShift.ShiftEndTime = shift.EndTime;

          // Set ShiftDefinitionId menjadi null
          bookingShift.ShiftDefinitionId = null;
        }

        // Simpan perubahan agar data historis tersimpan
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated {Count} booking shifts to preserve historical data for shift {ShiftName}",
            shift.BookingShifts.Count, shift.Name);
      }

      // Sekarang bisa hapus ShiftDefinition dengan aman
      _context.ShiftDefinitions.Remove(shift);
      await _context.SaveChangesAsync();

      _logger.LogInformation("Deleted shift definition {ShiftName} (ID: {Id})", shift.Name, id);
    }

    public async Task<bool> ShiftDefinitionExistsAsync(int id)
    {
      return await _context.ShiftDefinitions.AnyAsync(s => s.Id == id);
    }
  }
}
