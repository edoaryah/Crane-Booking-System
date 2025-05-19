using Microsoft.AspNetCore.Mvc.Rendering;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.ViewModels.CraneUsage;

namespace AspnetCoreMvcFull.Services.CraneUsage
{
  /// <summary>
  /// Interface for crane usage service operations
  /// </summary>
  public interface ICraneUsageService
  {
    /// <summary>
    /// Gets a filtered list of crane usage records
    /// </summary>
    /// <param name="filter">Filter parameters</param>
    /// <returns>View model with filtered records</returns>
    Task<CraneUsageRecordListViewModel> GetFilteredUsageRecordsAsync(CraneUsageFilterViewModel filter);

    /// <summary>
    /// Gets entries for a specific crane and date
    /// </summary>
    /// <param name="craneId">Crane ID</param>
    /// <param name="date">Date</param>
    /// <returns>List of entries</returns>
    Task<List<CraneUsageEntryViewModel>> GetCraneUsageEntriesForDateAsync(int craneId, DateTime date);

    /// <summary>
    /// Adds a new usage entry
    /// </summary>
    /// <param name="craneId">Crane ID</param>
    /// <param name="date">Date</param>
    /// <param name="entry">Entry data</param>
    /// <param name="userName">Username of creator</param>
    /// <returns>True if successful</returns>
    Task<bool> AddCraneUsageEntryAsync(int craneId, DateTime date, CraneUsageEntryViewModel entry, string userName);

    /// <summary>
    /// Updates an existing usage entry
    /// </summary>
    /// <param name="entry">Updated entry data</param>
    /// <returns>True if successful</returns>
    Task<bool> UpdateCraneUsageEntryAsync(CraneUsageEntryViewModel entry);

    /// <summary>
    /// Deletes a usage entry
    /// </summary>
    /// <param name="id">Entry ID</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteCraneUsageEntryAsync(int id);

    /// <summary>
    /// Gets an entry by ID
    /// </summary>
    /// <param name="id">Entry ID</param>
    /// <returns>Entry view model</returns>
    Task<CraneUsageEntryViewModel> GetCraneUsageEntryByIdAsync(int id);

    /// <summary>
    /// Gets an entry by time parameters
    /// </summary>
    /// <param name="craneId">Crane ID</param>
    /// <param name="date">Date</param>
    /// <param name="startTime">Start time</param>
    /// <param name="endTime">End time</param>
    /// <returns>Entry view model</returns>
    Task<CraneUsageEntryViewModel> GetCraneUsageEntryByTimeAsync(int craneId, DateTime date, TimeSpan startTime, TimeSpan endTime);

    /// <summary>
    /// Gets subcategories for a specific category
    /// </summary>
    /// <param name="category">Usage category</param>
    /// <returns>List of subcategories as select items</returns>
    Task<List<SelectListItem>> GetSubcategoriesByCategoryAsync(UsageCategory category);

    /// <summary>
    /// Gets available bookings for a time range
    /// </summary>
    /// <param name="craneId">Crane ID</param>
    /// <param name="startDateTime">Start date and time</param>
    /// <param name="endDateTime">End date and time</param>
    /// <returns>List of bookings as select items</returns>
    Task<List<SelectListItem>> GetAvailableBookingsAsync(int craneId, DateTime startDateTime, DateTime endDateTime);

    /// <summary>
    /// Saves all entries for a crane and date
    /// </summary>
    /// <param name="viewModel">Form data</param>
    /// <param name="userName">Username of editor</param>
    /// <returns>True if successful</returns>
    Task<bool> SaveCraneUsageFormAsync(CraneUsageFormViewModel viewModel, string userName);

    /// <summary>
    /// Finalizes a crane usage record
    /// </summary>
    /// <param name="craneId">Crane ID</param>
    /// <param name="date">Date</param>
    /// <param name="userName">Username of finalizer</param>
    /// <returns>True if successful</returns>
    Task<bool> FinalizeRecordAsync(int craneId, DateTime date, string userName);

    /// <summary>
    /// Gets data for minute visualization
    /// </summary>
    /// <param name="craneId">Crane ID</param>
    /// <param name="date">Date</param>
    /// <returns>Visualization view model</returns>
    Task<CraneUsageMinuteVisualizationViewModel> GetMinuteVisualizationDataAsync(int craneId, DateTime date);

    /// <summary>
    /// Validates that a new entry doesn't conflict with existing entries
    /// </summary>
    /// <param name="existingEntries">Existing entries</param>
    /// <param name="newEntry">New or updated entry</param>
    /// <returns>True if no conflicts</returns>
    bool ValidateNoTimeConflicts(List<CraneUsageEntryViewModel> existingEntries, CraneUsageEntryViewModel newEntry);

    /// <summary>
    /// Validates that no entries in a list conflict with each other
    /// </summary>
    /// <param name="entries">List of entries to check</param>
    /// <returns>True if no conflicts</returns>
    bool ValidateNoTimeConflicts(List<CraneUsageEntryViewModel> entries);

    /// <summary>
    /// Saves booking-specific usage entries
    /// </summary>
    /// <param name="viewModel">Form data</param>
    /// <param name="userName">Username of editor</param>
    /// <returns>True if successful</returns>
    Task<bool> SaveBookingUsageFormAsync(BookingUsageFormViewModel viewModel, string userName);
  }
}
