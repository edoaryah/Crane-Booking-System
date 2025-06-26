using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using AspnetCoreMvcFull.Filters;
using AspnetCoreMvcFull.Services;
using AspnetCoreMvcFull.Services.Role;
using AspnetCoreMvcFull.ViewModels.BookingManagement;
using AspnetCoreMvcFull.Models;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace AspnetCoreMvcFull.Controllers
{
  [ServiceFilter(typeof(AuthorizationFilter))]
  public class BookingController : Controller
  {
    private readonly ICraneService _craneService;
    private readonly IShiftDefinitionService _shiftService;
    private readonly IHazardService _hazardService;
    private readonly IBookingService _bookingService;
    private readonly IRoleService _roleService;
    private readonly IEmployeeService _employeeService;
    private readonly IScheduleConflictService _scheduleConflictService;
    private readonly ILogger<BookingController> _logger;
    private readonly IFileStorageService _fileStorageService;

    public BookingController(
        ICraneService craneService,
        IShiftDefinitionService shiftService,
        IHazardService hazardService,
        IBookingService bookingService,
        IRoleService roleService,
        IEmployeeService employeeService,
        IScheduleConflictService scheduleConflictService,
        ILogger<BookingController> logger,
        IFileStorageService fileStorageService)
    {
      _craneService = craneService;
      _shiftService = shiftService;
      _hazardService = hazardService;
      _bookingService = bookingService;
      _roleService = roleService;
      _employeeService = employeeService;
      _scheduleConflictService = scheduleConflictService;
      _logger = logger;
      _fileStorageService = fileStorageService;
    }

    // GET: /Booking
    public async Task<IActionResult> Index()
    {
      // Bersihkan TempData umum untuk mencegah "bocor" dari controller lain
      TempData.Remove("SuccessMessage");
      TempData.Remove("ErrorMessage");

      // Menandai bahwa kita telah membaca TempData spesifik halaman ini
      bool hasSuccessMessage = TempData.ContainsKey("BookingFormSuccessMessage");
      bool hasDocumentNumber = TempData.ContainsKey("BookingDocumentNumber");
      bool hasErrorMessage = TempData.ContainsKey("BookingFormErrorMessage");

      if (hasSuccessMessage)
      {
        // Simpan ke TempData.Peek agar bisa dibaca di view tapi akan dihapus setelah request
        TempData.Keep("BookingFormSuccessMessage");
      }

      if (hasDocumentNumber)
      {
        // Simpan ke TempData.Peek agar bisa dibaca di view tapi akan dihapus setelah request
        TempData.Keep("BookingDocumentNumber");
      }

      if (hasErrorMessage)
      {
        TempData.Keep("BookingFormErrorMessage");
      }

      try
      {
        var viewModel = new BookingFormViewModel
        {
          AvailableCranes = await _craneService.GetAllCranesAsync(),
          ShiftDefinitions = await _shiftService.GetAllShiftDefinitionsAsync(),
          AvailableHazards = await _hazardService.GetAllHazardsAsync()
        };

        // ✅ PERBAIKAN: Tambahkan flag untuk membersihkan TempData termasuk error
        ViewData["CleanTempData"] = hasSuccessMessage || hasDocumentNumber || hasErrorMessage;

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading booking form");
        TempData["BookingFormErrorMessage"] = "Error loading booking form: " + ex.Message;
        return View("Error");
      }
    }

    // POST: /Booking/ClearTempData
    [HttpPost]
    public IActionResult ClearTempData()
    {
      // Hapus semua TempData yang digunakan oleh halaman Booking/Index
      TempData.Remove("BookingFormSuccessMessage");
      TempData.Remove("BookingDocumentNumber");
      TempData.Remove("BookingFormErrorMessage");

      return Ok(new { success = true });
    }

    // GET: /Booking/List
    public async Task<IActionResult> List()
    {
      try
      {
        var bookings = await _bookingService.GetAllBookingsAsync();

        var viewModel = new BookingListViewModel
        {
          Bookings = bookings,
          Title = "Booking List",
          SuccessMessage = TempData["BookingSuccessMessage"] as string,
          ErrorMessage = TempData["BookingErrorMessage"] as string
        };

        // Hapus TempData setelah digunakan
        TempData.Remove("BookingSuccessMessage");
        TempData.Remove("BookingErrorMessage");

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading bookings");
        TempData["BookingErrorMessage"] = "Error loading bookings: " + ex.Message;
        return View(new BookingListViewModel { Title = "Booking List", ErrorMessage = ex.Message });
      }
    }

    // public async Task<IActionResult> Details(string documentNumber)
    // {
    //   try
    //   {
    //     // Get the current user's information from claims
    //     var ldapUser = User.FindFirst("ldapuser")?.Value;
    //     var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

    //     if (string.IsNullOrEmpty(ldapUser))
    //     {
    //       _logger.LogWarning("User LDAP username not found in claims");
    //       return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Details", "Booking", new { documentNumber }) });
    //     }

    //     // Get booking details
    //     var booking = await _bookingService.GetBookingByDocumentNumberAsync(documentNumber);

    //     // Cek otorisasi: Apakah pengguna memiliki akses ke booking ini
    //     if (booking == null)
    //     {
    //       _logger.LogWarning("Booking dengan document number {documentNumber} tidak ditemukan", documentNumber);
    //       return NotFound();
    //     }

    //     // Cek otorisasi: Apakah pengguna adalah PIC, atau pembuat booking, atau punya akses admin
    //     bool isPic = await _roleService.UserHasRoleAsync(ldapUser, "pic");
    //     bool isAdmin = await _roleService.UserHasRoleAsync(ldapUser, "admin");
    //     bool isBookingCreator = booking.Name == userName;

    //     // Jika bukan PIC, admin, atau pembuat booking, tolak akses
    //     if (!isPic && !isAdmin && !isBookingCreator)
    //     {
    //       _logger.LogWarning("User {ldapUser} mencoba mengakses booking {documentNumber} tanpa otorisasi", ldapUser, documentNumber);
    //       return RedirectToAction("AccessDenied", "Auth");
    //     }

    //     // Pass role information to the view
    //     ViewData["IsPicRole"] = isPic;
    //     ViewData["IsAdminRole"] = isAdmin;
    //     ViewData["IsBookingCreator"] = isBookingCreator;

    //     // Pass the booking to the view
    //     return View(booking);
    //   }
    //   catch (KeyNotFoundException ex)
    //   {
    //     _logger.LogWarning(ex, "Booking dengan document number {documentNumber} tidak ditemukan", documentNumber);
    //     return NotFound();
    //   }
    //   catch (Exception ex)
    //   {
    //     _logger.LogError(ex, "Terjadi kesalahan saat memuat detail booking dengan document number {documentNumber}", documentNumber);
    //     TempData["BookingErrorMessage"] = "Terjadi kesalahan saat memuat detail booking. Silakan coba lagi.";
    //     return RedirectToAction(nameof(List));
    //   }
    // }

    public async Task<IActionResult> Details(string documentNumber)
    {
      try
      {
        // Get the current user's information from claims
        var ldapUser = User.FindFirst("ldapuser")?.Value;
        var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(ldapUser))
        {
          _logger.LogWarning("User LDAP username not found in claims");
          return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Details", "Booking", new { documentNumber }) });
        }

        // Get booking details
        var booking = await _bookingService.GetBookingByDocumentNumberAsync(documentNumber);

        if (booking == null)
        {
          _logger.LogWarning("Booking dengan document number {documentNumber} tidak ditemukan", documentNumber);
          return NotFound();
        }

        // ✅ GET USER ROLES - SAMA DENGAN BookingListController
        var userRoles = await _roleService.GetUserRolesAsync(ldapUser);

        bool isPic = userRoles.Contains("pic", StringComparer.OrdinalIgnoreCase);
        bool isAdmin = userRoles.Contains("admin", StringComparer.OrdinalIgnoreCase);
        bool isManager = userRoles.Contains("manager", StringComparer.OrdinalIgnoreCase);
        bool isBookingCreator = booking.LdapUser == ldapUser; // ✅ GUNAKAN LDAP, BUKAN NAME

        // ✅ IMPLEMENT ROLE-BASED ACCESS CONTROL
        bool hasAccess = false;
        string accessReason = "";

        if (isAdmin || isPic)
        {
          // Admin dan PIC bisa akses semua booking
          hasAccess = true;
          accessReason = isAdmin ? "Admin access" : "PIC access";
        }
        else if (isManager)
        {
          // Manager bisa akses booking dari departemennya
          var employee = await _employeeService.GetEmployeeByLdapUserAsync(ldapUser);
          if (employee != null && !string.IsNullOrEmpty(employee.Department) &&
              employee.Department == booking.Department)
          {
            hasAccess = true;
            accessReason = $"Manager access for department: {employee.Department}";
          }
        }
        else if (isBookingCreator)
        {
          // User biasa bisa akses booking yang mereka buat
          hasAccess = true;
          accessReason = "Booking creator access";
        }

        // ✅ REJECT AKSES JIKA TIDAK MEMENUHI KRITERIA
        if (!hasAccess)
        {
          _logger.LogWarning("User {ldapUser} dengan roles [{roles}] mencoba mengakses booking {documentNumber} tanpa otorisasi. Booking department: {department}",
                           ldapUser, string.Join(", ", userRoles), documentNumber, booking.Department);
          return RedirectToAction("AccessDenied", "Auth");
        }

        // ✅ LOG AKSES YANG BERHASIL
        _logger.LogInformation("User {ldapUser} mengakses booking {documentNumber}. Reason: {reason}",
                             ldapUser, documentNumber, accessReason);

        // ✅ PASS ROLE INFO TO VIEW
        ViewData["IsPicRole"] = isPic;
        ViewData["IsAdminRole"] = isAdmin;
        ViewData["IsManagerRole"] = isManager; // ✅ TAMBAH INFO MANAGER
        ViewData["IsBookingCreator"] = isBookingCreator;
        ViewData["UserRoles"] = userRoles; // ✅ UNTUK DEBUG

        return View(booking);
      }
      catch (KeyNotFoundException ex)
      {
        _logger.LogWarning(ex, "Booking dengan document number {documentNumber} tidak ditemukan", documentNumber);
        return NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Terjadi kesalahan saat memuat detail booking dengan document number {documentNumber}", documentNumber);
        TempData["BookingErrorMessage"] = "Terjadi kesalahan saat memuat detail booking. Silakan coba lagi.";
        return RedirectToAction("Index", "BookingList"); // ✅ REDIRECT KE BookingList, BUKAN List
      }
    }

    // POST: /Booking/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BookingCreateViewModel viewModel)
    {
      try
      {
        // ✅ AMBIL DATA USER DARI CLAIM DI SERVER-SIDE (AMAN)
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";
        var ldapUser = User.FindFirst("ldapuser")?.Value ?? string.Empty;
        var userDepartment = User.FindFirst("department")?.Value ?? string.Empty;

        // ✅ Validasi data user dari claim
        if (string.IsNullOrEmpty(ldapUser))
        {
          _logger.LogWarning("LDAP user not found in claims for user: {Name}", userName);
          TempData["BookingFormErrorMessage"] = "User authentication error. Please login again.";
          return RedirectToAction("Index");
        }

        // ✅ SET DATA USER KE VIEWMODEL (DARI CLAIM, BUKAN DARI FORM)
        viewModel.Name = userName;
        viewModel.LdapUser = ldapUser;
        viewModel.Department = userDepartment;

        _logger.LogInformation("Creating booking for user: {Name} (LDAP: {LdapUser})", userName, ldapUser);

        if (ModelState.IsValid)
        {
          // Handle image uploads
          var imagePaths = new List<string>();
          if (viewModel.Images != null && viewModel.Images.Count > 0)
          {
            foreach (var image in viewModel.Images)
            {
              if (image.Length > 0)
              {
                var savedPath = await _fileStorageService.SaveFileAsync(image, "booking-images");
                imagePaths.Add(savedPath);
              }
            }
          }

          var createdBooking = await _bookingService.CreateBookingAsync(viewModel, imagePaths);

          // Simpan data untuk ditampilkan di modal
          TempData["BookingFormSuccessMessage"] = "Booking berhasil dibuat";
          TempData["BookingDocumentNumber"] = createdBooking.DocumentNumber;

          // Redirect ke Index (bukan ke Details) agar modal ditampilkan dulu
          return RedirectToAction(nameof(Index));
        }

        // Jika validasi gagal, kembali ke form dengan data yang dibutuhkan
        var formViewModel = new BookingFormViewModel
        {
          AvailableCranes = await _craneService.GetAllCranesAsync(),
          ShiftDefinitions = await _shiftService.GetAllShiftDefinitionsAsync(),
          AvailableHazards = await _hazardService.GetAllHazardsAsync()
        };

        // Tambahkan pesan error
        ModelState.AddModelError("", "Silakan perbaiki error dan coba lagi.");

        return View("Index", formViewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error creating booking");

        // Redirect ke Index dengan pesan error
        TempData["BookingFormErrorMessage"] = "Error membuat booking: " + ex.Message;
        return RedirectToAction("Index");
      }
    }

    // // GET: /Booking/Edit/{documentNumber}
    // public async Task<IActionResult> Edit(string documentNumber)
    // {
    //   try
    //   {
    //     var booking = await _bookingService.GetBookingByDocumentNumberAsync(documentNumber);

    //     // Convert to update view model
    //     var viewModel = new BookingUpdateViewModel
    //     {
    //       Name = booking.Name,
    //       Department = booking.Department,
    //       CraneId = booking.CraneId,
    //       StartDate = booking.StartDate,
    //       EndDate = booking.EndDate,
    //       Location = booking.Location,
    //       ProjectSupervisor = booking.ProjectSupervisor,
    //       CostCode = booking.CostCode,
    //       PhoneNumber = booking.PhoneNumber,
    //       Description = booking.Description,
    //       CustomHazard = booking.CustomHazard,
    //       ShiftSelections = ConvertShiftsToSelections(booking),
    //       Items = booking.Items.Select(i => new BookingItemCreateViewModel
    //       {
    //         ItemName = i.ItemName,
    //         Weight = i.Weight,
    //         Height = i.Height,
    //         Quantity = i.Quantity
    //       }).ToList(),
    //       HazardIds = booking.SelectedHazards.Select(h => h.Id).ToList()
    //     };

    //     ViewBag.Cranes = await _craneService.GetAllCranesAsync();
    //     ViewBag.ShiftDefinitions = await _shiftService.GetAllShiftDefinitionsAsync();
    //     ViewBag.Hazards = await _hazardService.GetAllHazardsAsync();
    //     ViewBag.DocumentNumber = booking.DocumentNumber;
    //     ViewBag.BookingId = booking.Id;

    //     return View(viewModel);
    //   }
    //   catch (KeyNotFoundException)
    //   {
    //     return NotFound();
    //   }
    //   catch (Exception ex)
    //   {
    //     _logger.LogError(ex, "Error loading booking for edit with document number: {DocumentNumber}", documentNumber);
    //     TempData["BookingErrorMessage"] = "Error loading booking: " + ex.Message;
    //     return RedirectToAction(nameof(List));
    //   }
    // }

    // POST: /Booking/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, BookingUpdateViewModel viewModel)
    {
      try
      {
        if (ModelState.IsValid)
        {
          var booking = await _bookingService.GetBookingByIdAsync(id);
          if (booking == null)
          {
            return NotFound();
          }

          string currentUserName = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("ldapuser")?.Value ?? "System";
          var existingImagePaths = booking.ImagePaths?.ToList() ?? new List<string>();
          var updatedBooking = await _bookingService.UpdateBookingAsync(id, viewModel, currentUserName, existingImagePaths);

          TempData["BookingSuccessMessage"] = "Booking berhasil diperbarui";
          return RedirectToAction(nameof(Details), new { documentNumber = updatedBooking.DocumentNumber });
        }

        // If model state is invalid, redisplay form with existing data
        ViewBag.Cranes = await _craneService.GetAllCranesAsync();
        ViewBag.ShiftDefinitions = await _shiftService.GetAllShiftDefinitionsAsync();
        ViewBag.Hazards = await _hazardService.GetAllHazardsAsync();

        var bookingToUpdate = await _bookingService.GetBookingByIdAsync(id);
        ViewBag.DocumentNumber = bookingToUpdate.DocumentNumber;
        ViewBag.BookingId = id;

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error updating booking with ID: {Id}", id);
        ModelState.AddModelError("", "Error updating booking: " + ex.Message);

        // Repopulate view bags for the view
        ViewBag.Cranes = await _craneService.GetAllCranesAsync();
        ViewBag.ShiftDefinitions = await _shiftService.GetAllShiftDefinitionsAsync();
        ViewBag.Hazards = await _hazardService.GetAllHazardsAsync();

        var booking = await _bookingService.GetBookingByIdAsync(id);
        ViewBag.DocumentNumber = booking.DocumentNumber;
        ViewBag.BookingId = id;

        return View(viewModel);
      }
    }

    // GET: /Booking/Delete/{documentNumber}
    public async Task<IActionResult> Delete(string documentNumber)
    {
      try
      {
        var booking = await _bookingService.GetBookingByDocumentNumberAsync(documentNumber);
        return View(booking);
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading booking for deletion with document number: {DocumentNumber}", documentNumber);
        TempData["BookingErrorMessage"] = "Error loading booking: " + ex.Message;
        return RedirectToAction(nameof(List));
      }
    }

    // GET: /Booking/History
    public async Task<IActionResult> History()
    {
      try
      {
        var bookings = await _bookingService.GetAllBookingsAsync();

        var viewModel = new BookingListViewModel
        {
          Bookings = bookings,
          Title = "Booking History",
          SuccessMessage = TempData["BookingSuccessMessage"] as string,
          ErrorMessage = TempData["BookingErrorMessage"] as string
        };

        // Hapus TempData setelah digunakan
        TempData.Remove("BookingSuccessMessage");
        TempData.Remove("BookingErrorMessage");

        return View("List", viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading booking history");
        TempData["BookingErrorMessage"] = "Error loading booking history: " + ex.Message;
        return View("List", new BookingListViewModel
        {
          Title = "Booking History",
          ErrorMessage = ex.Message
        });
      }
    }

    // POST: /Booking/Delete/{id}
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
      try
      {
        await _bookingService.DeleteBookingAsync(id);

        TempData["BookingSuccessMessage"] = "Booking berhasil dihapus";
        return RedirectToAction(nameof(List));
      }
      catch (KeyNotFoundException)
      {
        return NotFound();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error deleting booking with ID: {Id}", id);
        TempData["BookingErrorMessage"] = "Error deleting booking: " + ex.Message;
        return RedirectToAction(nameof(List));
      }
    }

    // GET: /Booking/Calendar atau /Calendar
    [Route("Booking/Calendar")]
    [Route("Calendar")] // Menambahkan route tambahan
    public async Task<IActionResult> Calendar(DateTime? startDate = null, DateTime? endDate = null)
    {
      try
      {
        // Jika startDate tidak disediakan, gunakan hari ini
        DateTime start = startDate?.Date ?? DateTime.Now.Date;

        // Jika endDate tidak disediakan, gunakan 6 hari setelah startDate (total 7 hari)
        DateTime end = endDate?.Date ?? start.AddDays(6);

        // Ambil data kalender
        var calendarData = await _bookingService.GetCalendarViewAsync(start, end);

        // Pass role info to ViewData for calendar view
        bool isPic = User.IsInRole("PIC");
        bool isAdmin = User.IsInRole("Admin");
        ViewData["IsPicRole"] = isPic;
        ViewData["IsAdminRole"] = isAdmin;

        // Ambil data shift definitions aktif dan kirim ke view via ViewBag
        var allShifts = await _shiftService.GetAllShiftDefinitionsAsync();
        ViewBag.ShiftDefinitions = allShifts.Where(s => s.IsActive).ToList();

        return View(calendarData);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading calendar view");
        TempData["BookingErrorMessage"] = "Error loading calendar: " + ex.Message;
        return View("Error");
      }
    }

    // GET: /Booking/Approved
    public async Task<IActionResult> Approved()
    {
      try
      {
        var approvedBookings = await _bookingService.GetBookingsByStatusAsync(BookingStatus.PICApproved);

        var viewModel = new BookingListViewModel
        {
          Bookings = approvedBookings,
          Title = "Approved Bookings",
          SuccessMessage = TempData["BookingSuccessMessage"] as string,
          ErrorMessage = TempData["BookingErrorMessage"] as string
        };

        // Hapus TempData setelah digunakan
        TempData.Remove("BookingSuccessMessage");
        TempData.Remove("BookingErrorMessage");

        return View("List", viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error loading approved bookings");
        TempData["BookingErrorMessage"] = "Error loading approved bookings: " + ex.Message;
        return View(new BookingListViewModel
        {
          Title = "Approved Bookings",
          ErrorMessage = ex.Message
        });
      }
    }

    // GET: /Booking/GetBookedShifts
    // [HttpGet]
    // public async Task<IActionResult> GetBookedShifts(int craneId, DateTime startDate, DateTime endDate)
    // {
    //   try
    //   {
    //     var bookedShifts = await _bookingService.GetBookedShiftsByCraneAndDateRangeAsync(
    //         craneId, startDate, endDate);
    //     return Json(bookedShifts);
    //   }
    //   catch (Exception ex)
    //   {
    //     _logger.LogError(ex, "Error getting booked shifts");
    //     return StatusCode(500, new { error = ex.Message });
    //   }
    // }

    // GET: /Booking/GetBookedShifts
    [HttpGet]
    public async Task<IActionResult> GetBookedShifts(int craneId, DateTime startDate, DateTime endDate, int? excludeBookingId = null)
    {
      try
      {
        var bookedShifts = await _bookingService.GetBookedShiftsByCraneAndDateRangeAsync(
            craneId, startDate, endDate, excludeBookingId); // Tambahkan parameter excludeBookingId
        return Json(bookedShifts);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting booked shifts");
        return StatusCode(500, new { error = ex.Message });
      }
    }

    // GET: /Booking/CheckShiftConflict
    [HttpGet]
    public async Task<IActionResult> CheckShiftConflict(
        int craneId,
        DateTime date,
        int shiftDefinitionId,
        int? excludeBookingId = null)
    {
      try
      {
        var hasConflict = await _bookingService.IsShiftBookingConflictAsync(
            craneId, date, shiftDefinitionId, excludeBookingId);

        return Json(new { hasConflict });
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error checking shift conflict");
        return StatusCode(500, new { error = ex.Message });
      }
    }

    // Helper methods
    private List<DailyShiftSelectionViewModel> ConvertShiftsToSelections(BookingDetailViewModel booking)
    {
      // Group shifts by date
      var groupedShifts = booking.Shifts.GroupBy(s => s.Date.Date)
                                     .Select(g => new
                                     {
                                       Date = g.Key,
                                       ShiftIds = g.Select(s => s.ShiftDefinitionId).ToList()
                                     })
                                     .ToList();

      // Convert to selection view models
      return groupedShifts.Select(g => new DailyShiftSelectionViewModel
      {
        Date = g.Date,
        SelectedShiftIds = g.ShiftIds
      }).ToList();
    }

    // GET: /Booking/Search
    [HttpGet]
    public async Task<IActionResult> Search(string searchTerm)
    {
      try
      {
        // Return to home page if search term is empty
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
          return RedirectToAction("Index", "Dashboards");
        }

        // Get current user's LDAP
        var ldapUser = User.FindFirst("ldapuser")?.Value;
        var userName = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(ldapUser))
        {
          _logger.LogWarning("User LDAP username not found in claims during search");
          return RedirectToAction("Login", "Auth", new { returnUrl = Url.Action("Search", "Booking", new { searchTerm }) });
        }

        // Check user roles for access control
        bool isPic = await _roleService.UserHasRoleAsync(ldapUser, "pic");
        bool isAdmin = await _roleService.UserHasRoleAsync(ldapUser, "admin");

        // Get search results
        var searchResults = await _bookingService.SearchBookingsAsync(searchTerm, ldapUser, isPic, isAdmin);

        // Create view model for results
        var viewModel = new BookingSearchViewModel
        {
          SearchTerm = searchTerm,
          Bookings = searchResults,
          SuccessMessage = TempData["BookingSuccessMessage"] as string,
          ErrorMessage = TempData["BookingErrorMessage"] as string
        };

        // Clear TempData after use
        TempData.Remove("BookingSuccessMessage");
        TempData.Remove("BookingErrorMessage");

        return View(viewModel);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error searching bookings with term: {SearchTerm}", searchTerm);
        TempData["BookingErrorMessage"] = "Error searching bookings: " + ex.Message;
        return RedirectToAction("Index", "Dashboards");
      }
    }

    // [HttpGet]
    // public async Task<IActionResult> GetMaintenanceShifts(int craneId, DateTime startDate, DateTime endDate)
    // {
    //   try
    //   {
    //     var maintenanceShifts = new List<BookedShiftViewModel>();

    //     // Loop melalui rentang tanggal yang diminta
    //     var currentDate = startDate.Date;
    //     while (currentDate <= endDate.Date)
    //     {
    //       // Loop melalui semua shift definitions
    //       foreach (var shift in await _shiftService.GetAllShiftDefinitionsAsync())
    //       {
    //         // Periksa apakah ada konflik maintenance
    //         bool hasMaintenanceConflict = await _scheduleConflictService.IsMaintenanceConflictAsync(
    //             craneId, currentDate, shift.Id);

    //         if (hasMaintenanceConflict)
    //         {
    //           // Tambahkan ke daftar
    //           maintenanceShifts.Add(new BookedShiftViewModel
    //           {
    //             CraneId = craneId,
    //             Date = currentDate,
    //             ShiftDefinitionId = shift.Id
    //           });
    //         }
    //       }

    //       currentDate = currentDate.AddDays(1);
    //     }

    //     return Json(maintenanceShifts);
    //   }
    //   catch (Exception ex)
    //   {
    //     _logger.LogError(ex, "Error getting maintenance shifts");
    //     return StatusCode(500, new { error = ex.Message });
    //   }
    // }
    [HttpGet]
    public async Task<IActionResult> GetMaintenanceShifts(int craneId, DateTime startDate, DateTime endDate)
    {
      try
      {
        // Dapatkan Crane Code dari CraneService
        var crane = await _craneService.GetCraneByIdAsync(craneId);
        string craneCode = crane?.Code ?? string.Empty;

        var maintenanceShifts = new List<BookedShiftViewModel>();

        // Loop melalui rentang tanggal yang diminta
        var currentDate = startDate.Date;
        while (currentDate <= endDate.Date)
        {
          // Loop melalui semua shift definitions
          foreach (var shift in await _shiftService.GetAllShiftDefinitionsAsync())
          {
            // Periksa apakah ada konflik maintenance
            bool hasMaintenanceConflict = await _scheduleConflictService.IsMaintenanceConflictAsync(
                craneId, currentDate, shift.Id, null, craneCode);

            if (hasMaintenanceConflict)
            {
              // Tambahkan ke daftar
              maintenanceShifts.Add(new BookedShiftViewModel
              {
                CraneId = craneId,
                Date = currentDate,
                ShiftDefinitionId = shift.Id
              });
            }
          }

          currentDate = currentDate.AddDays(1);
        }

        return Json(maintenanceShifts);
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting maintenance shifts");
        return StatusCode(500, new { error = ex.Message });
      }
    }
  }
}
