using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Data;
// using AspnetCoreMvcFull.Events;
// using AspnetCoreMvcFull.Events.Handlers;
using AspnetCoreMvcFull.Filters;
using AspnetCoreMvcFull.Middleware;
using AspnetCoreMvcFull.Services.Auth;
using AspnetCoreMvcFull.Services.Common;
using AspnetCoreMvcFull.Services.Role;
using AspnetCoreMvcFull.Services.Security;
using AspnetCoreMvcFull.Services.CraneUsage;
using AspnetCoreMvcFull.Services.Billing;
using AspnetCoreMvcFull.Services;
using System.Text;
using Hangfire;
using Hangfire.PostgreSql;

var builder = WebApplication.CreateBuilder(args);

// Tambahkan ini di bagian awal Program.cs, sebelum kode konfigurasi apapun
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

// Add services to the container
builder.Services.AddControllersWithViews();

// Register DbContext dengan connection string
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

// Registrasi Filter
builder.Services.AddScoped<AuthorizationFilter>();
builder.Services.AddScoped<RateLimitFilter>();

// Tambahkan setelah registrasi service yang ada
// Registrasi event infrastructure
// builder.Services.AddSingleton<IEventPublisher, EventPublisher>();
// builder.Services.AddScoped<IEventHandler<CraneMaintenanceEvent>, BookingRelocationHandler>();

builder.Services.AddScoped<IHazardService, HazardService>();
builder.Services.AddScoped<IShiftDefinitionService, ShiftDefinitionService>();

// Menambahkan HttpClient untuk digunakan di AuthService
builder.Services.AddHttpClient("AuthClient", client =>
{
  client.Timeout = TimeSpan.FromSeconds(30);
});

// Register memory cache untuk penyimpanan auth-related data
builder.Services.AddMemoryCache();

// Register auth dan security services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICacheService, MemoryCacheService>();
builder.Services.AddScoped<ISecurityService, SecurityService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

builder.Services.AddScoped<IScheduleConflictService, ScheduleConflictService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<ICraneService, CraneService>();
builder.Services.AddScoped<IMaintenanceScheduleService, MaintenanceScheduleService>();

// Tambahkan di Program.cs sebelum app.Build()
builder.Services.AddScoped<AspnetCoreMvcFull.Services.Dashboard.IDashboardService, AspnetCoreMvcFull.Services.Dashboard.DashboardService>();

// Register Role service
builder.Services.AddScoped<IRoleService, RoleService>();

// Register Crane Usage Record service
builder.Services.AddScoped<ICraneUsageService, CraneUsageService>();

builder.Services.AddScoped<IUsageSubcategoryService, UsageSubcategoryService>();

// Di Program.cs tambahkan:
builder.Services.AddScoped<IBillingService, BillingService>();

// Daftarkan EmailTemplate sebagai singleton karena hanya berisi template
builder.Services.AddSingleton<EmailTemplate>();

// Daftarkan layanan email
builder.Services.AddScoped<IEmailService, EmailService>();

// Daftarkan layanan karyawan
builder.Services.AddScoped<IEmployeeService, EmployeeService>();

// Program.cs - tambahkan baris berikut di bagian pendaftaran layanan
builder.Services.AddScoped<IBookingApprovalService, BookingApprovalService>();

// Konfigurasi Autentikasi
builder.Services.AddAuthentication(options =>
{
  options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
  options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
  options.Cookie.Name = "CraneBookingAuth";
  options.Cookie.HttpOnly = true;
  options.ExpireTimeSpan = TimeSpan.FromHours(8);
  options.SlidingExpiration = true;
  options.LoginPath = "/Auth/Login";
  options.LogoutPath = "/Auth/Logout";
  options.AccessDeniedPath = "/Auth/AccessDenied";
  options.Cookie.SameSite = SameSiteMode.Lax;  // Ubah ke Lax
  options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;  // Ubah untuk pengembangan

  // Event untuk validasi cookie dan automatic refresh token
  options.Events = new CookieAuthenticationEvents
  {
    OnValidatePrincipal = async context =>
    {
      // Cek apakah cookie akan segera kedaluwarsa
      if (context.Properties.AllowRefresh != true) return;

      var timeElapsed = DateTimeOffset.UtcNow.Subtract(context.Properties.IssuedUtc ?? DateTimeOffset.UtcNow);
      if (timeElapsed > TimeSpan.FromHours(7)) // Refresh setelah 7 jam (sebelum 8 jam kedaluwarsa)
      {
        var authService = context.HttpContext.RequestServices.GetRequiredService<IAuthService>();
        if (context.HttpContext.Request.Cookies.TryGetValue("jwt_token", out var token) &&
                    context.HttpContext.Request.Cookies.TryGetValue("refresh_token", out var refreshToken))
        {
          var response = await authService.RefreshTokenAsync(token, refreshToken);
          if (!response.Success)
          {
            context.RejectPrincipal();
            return;
          }

          // Update cookies
          context.HttpContext.Response.Cookies.Append("jwt_token", response.Token, new CookieOptions
          {
            HttpOnly = true,
            Secure = false,  // Ubah ke false untuk pengembangan
            SameSite = SameSiteMode.Lax,  // Ubah ke Lax
            Expires = response.TokenExpires
          });

          context.HttpContext.Response.Cookies.Append("refresh_token", response.RefreshToken, new CookieOptions
          {
            HttpOnly = true,
            Secure = false,  // Ubah ke false untuk pengembangan
            SameSite = SameSiteMode.Lax,  // Ubah ke Lax
            Expires = DateTime.UtcNow.AddDays(7)
          });

          // Update properti authentication ticket
          context.Properties.IssuedUtc = DateTimeOffset.UtcNow;
          context.Properties.ExpiresUtc = DateTimeOffset.UtcNow.Add(options.ExpireTimeSpan);
          context.ShouldRenew = true;
        }
        else
        {
          context.RejectPrincipal();
        }
      }
    }
  };
});

// hangfire
builder.Services.AddHangfire(config =>
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
          .UseSimpleAssemblyNameTypeSerializer()
          .UsePostgreSqlStorage(options =>
              options.UseNpgsqlConnection(builder.Configuration.GetConnectionString("DefaultConnection"))
          ));

builder.Services.AddHangfireServer();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
  app.UseExceptionHandler("/Home/Error");
  // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
  app.UseHsts();
}

app.UseHangfireDashboard(); // Dashboard untuk memonitor background jobs dengan Hangfire

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Tambahkan middleware autentikasi SEBELUM middleware otorisasi
app.UseAuthentication();
app.UseAuthorization();

// Tambahkan middleware token refresh
app.UseTokenRefresh();

// Log aktivitas keamanan untuk resource terproteksi
app.Use(async (context, next) =>
{
  // Log semua upaya akses ke resource yang terproteksi
  if (context.Request.Path.StartsWithSegments("/Admin") ||
      context.Request.Path.StartsWithSegments("/Management"))
  {
    var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
    logger.LogInformation(
        "Upaya akses ke resource terproteksi: {Path} oleh {User} dari {IP}",
        context.Request.Path,
        context.User.Identity?.Name ?? "anonymous",
        context.Connection.RemoteIpAddress);
  }

  await next();
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Dashboards}/{action=Index}/{id?}");

app.Run();
