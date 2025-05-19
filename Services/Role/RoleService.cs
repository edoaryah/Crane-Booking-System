using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Data;
using AspnetCoreMvcFull.Models.Role;
using AspnetCoreMvcFull.Models.Auth;
using AspnetCoreMvcFull.ViewModels.Role;
using AspnetCoreMvcFull.Services.Auth;

namespace AspnetCoreMvcFull.Services.Role
{
  public class RoleService : IRoleService
  {
    private readonly AppDbContext _context;
    private readonly ILogger<RoleService> _logger;
    private readonly string _sqlServerConnectionString;
    private readonly IAuthService _authService;

    public RoleService(
        AppDbContext dbContext,
        ILogger<RoleService> logger,
        IConfiguration configuration,
        IAuthService authService)
    {
      _context = dbContext;
      _logger = logger;
      _sqlServerConnectionString = configuration.GetConnectionString("SqlServerConnection") ??
          throw new InvalidOperationException("SqlServerConnection is not configured");
      _authService = authService;
    }

    #region Role Management

    public async Task<List<RoleSummaryViewModel>> GetAllRolesAsync()
    {
      try
      {
        var roles = new List<RoleSummaryViewModel>();

        // Create summary for each predefined role
        foreach (var roleName in Roles.AllRoles)
        {
          // Count users in this role
          int userCount = await _context.UserRoles
              .Where(r => r.RoleName.ToLower() == roleName.ToLower())
              .CountAsync();

          roles.Add(new RoleSummaryViewModel
          {
            Name = roleName,
            Description = Roles.RoleDescriptions.TryGetValue(roleName, out var desc) ? desc : null,
            UserCount = userCount
          });
        }

        return roles;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting all roles");
        throw;
      }
    }

    public async Task<RoleSummaryViewModel?> GetRoleByNameAsync(string name)
    {
      try
      {
        // Check if the role name is valid
        if (!Roles.AllRoles.Contains(name.ToLower()))
        {
          return null;
        }

        // Count users in this role
        int userCount = await _context.UserRoles
            .Where(r => r.RoleName.ToLower() == name.ToLower())
            .CountAsync();

        return new RoleSummaryViewModel
        {
          Name = name,
          Description = Roles.RoleDescriptions.TryGetValue(name, out var desc) ? desc : null,
          UserCount = userCount
        };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting role by name: {Name}", name);
        throw;
      }
    }

    public Task<bool> IsRoleValidAsync(string roleName)
    {
      return Task.FromResult(Roles.AllRoles.Contains(roleName.ToLower()));
    }

    #endregion

    #region User-Role Management

    public async Task<List<UserRoleViewModel>> GetUsersByRoleNameAsync(string roleName)
    {
      try
      {
        var userRoles = await _context.UserRoles
            .Where(ur => ur.RoleName.ToLower() == roleName.ToLower())
            .ToListAsync();

        var userRoleDtos = new List<UserRoleViewModel>();

        foreach (var userRole in userRoles)
        {
          var employee = await _authService.GetEmployeeByLdapUserAsync(userRole.LdapUser);

          userRoleDtos.Add(new UserRoleViewModel
          {
            Id = userRole.Id,
            LdapUser = userRole.LdapUser,
            RoleName = userRole.RoleName,
            Notes = userRole.Notes != null ? System.Web.HttpUtility.HtmlDecode(userRole.Notes) : null,
            CreatedAt = userRole.CreatedAt,
            CreatedBy = userRole.CreatedBy,
            UpdatedAt = userRole.UpdatedAt,
            UpdatedBy = userRole.UpdatedBy,
            EmployeeName = employee?.Name,
            EmployeeId = employee?.EmpId,
            Department = employee?.Department != null ?
                System.Web.HttpUtility.HtmlDecode(employee.Department) : null,
            Position = employee?.PositionTitle != null ?
                  System.Web.HttpUtility.HtmlDecode(employee.PositionTitle) : null
          });
        }

        return userRoleDtos;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting users by role name: {RoleName}", roleName);
        throw;
      }
    }

    public async Task<UserRoleViewModel?> GetUserRoleByIdAsync(int id)
    {
      try
      {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.Id == id);

        if (userRole == null)
        {
          return null;
        }

        var employee = await _authService.GetEmployeeByLdapUserAsync(userRole.LdapUser);

        return new UserRoleViewModel
        {
          Id = userRole.Id,
          LdapUser = userRole.LdapUser,
          RoleName = userRole.RoleName,
          Notes = userRole.Notes,
          CreatedAt = userRole.CreatedAt,
          CreatedBy = userRole.CreatedBy,
          UpdatedAt = userRole.UpdatedAt,
          UpdatedBy = userRole.UpdatedBy,
          EmployeeName = employee?.Name,
          EmployeeId = employee?.EmpId,
          Department = employee?.Department,
          Position = employee?.PositionTitle
        };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting user role by ID: {Id}", id);
        throw;
      }
    }

    public async Task<UserRoleViewModel?> GetUserRoleByLdapUserAndRoleNameAsync(string ldapUser, string roleName)
    {
      try
      {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.LdapUser == ldapUser && ur.RoleName.ToLower() == roleName.ToLower());

        if (userRole == null)
        {
          return null;
        }

        var employee = await _authService.GetEmployeeByLdapUserAsync(userRole.LdapUser);

        return new UserRoleViewModel
        {
          Id = userRole.Id,
          LdapUser = userRole.LdapUser,
          RoleName = userRole.RoleName,
          Notes = userRole.Notes,
          CreatedAt = userRole.CreatedAt,
          CreatedBy = userRole.CreatedBy,
          UpdatedAt = userRole.UpdatedAt,
          UpdatedBy = userRole.UpdatedBy,
          EmployeeName = employee?.Name,
          EmployeeId = employee?.EmpId,
          Department = employee?.Department,
          Position = employee?.PositionTitle
        };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting user role by LDAP user and role name: {LdapUser}, {RoleName}", ldapUser, roleName);
        throw;
      }
    }

    public async Task<UserRoleViewModel> AssignRoleToUserAsync(UserRoleCreateViewModel userRoleDto, string createdBy)
    {
      try
      {
        // Verify the role is valid
        if (!Roles.AllRoles.Contains(userRoleDto.RoleName.ToLower()))
        {
          throw new KeyNotFoundException($"Role {userRoleDto.RoleName} not found");
        }

        // Check if the user already has this role
        var existingUserRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.LdapUser == userRoleDto.LdapUser &&
                                      ur.RoleName.ToLower() == userRoleDto.RoleName.ToLower());

        if (existingUserRole != null)
        {
          throw new InvalidOperationException($"User {userRoleDto.LdapUser} already has the role {userRoleDto.RoleName}");
        }

        // Verify the user exists in employee database
        var employee = await _authService.GetEmployeeByLdapUserAsync(userRoleDto.LdapUser);
        if (employee == null)
        {
          throw new KeyNotFoundException($"Employee with LDAP user {userRoleDto.LdapUser} not found");
        }

        var userRole = new UserRole
        {
          LdapUser = userRoleDto.LdapUser,
          RoleName = userRoleDto.RoleName,
          Notes = userRoleDto.Notes,
          CreatedAt = DateTime.Now,
          CreatedBy = createdBy
        };

        _context.UserRoles.Add(userRole);
        await _context.SaveChangesAsync();

        return new UserRoleViewModel
        {
          Id = userRole.Id,
          LdapUser = userRole.LdapUser,
          RoleName = userRole.RoleName,
          Notes = userRole.Notes,
          CreatedAt = userRole.CreatedAt,
          CreatedBy = userRole.CreatedBy,
          UpdatedAt = userRole.UpdatedAt,
          UpdatedBy = userRole.UpdatedBy,
          EmployeeName = employee.Name,
          EmployeeId = employee.EmpId,
          Department = employee.Department,
          Position = employee.PositionTitle
        };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error assigning role to user: {LdapUser}, {RoleName}", userRoleDto.LdapUser, userRoleDto.RoleName);
        throw;
      }
    }

    public async Task<UserRoleViewModel> UpdateUserRoleAsync(int id, UserRoleUpdateViewModel userRoleDto, string updatedBy)
    {
      try
      {
        var userRole = await _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.Id == id);

        if (userRole == null)
        {
          throw new KeyNotFoundException($"User role with ID {id} not found");
        }

        userRole.Notes = userRoleDto.Notes;
        userRole.UpdatedAt = DateTime.Now;
        userRole.UpdatedBy = updatedBy;

        _context.UserRoles.Update(userRole);
        await _context.SaveChangesAsync();

        var employee = await _authService.GetEmployeeByLdapUserAsync(userRole.LdapUser);

        return new UserRoleViewModel
        {
          Id = userRole.Id,
          LdapUser = userRole.LdapUser,
          RoleName = userRole.RoleName,
          Notes = userRole.Notes,
          CreatedAt = userRole.CreatedAt,
          CreatedBy = userRole.CreatedBy,
          UpdatedAt = userRole.UpdatedAt,
          UpdatedBy = userRole.UpdatedBy,
          EmployeeName = employee?.Name,
          EmployeeId = employee?.EmpId,
          Department = employee?.Department,
          Position = employee?.PositionTitle
        };
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error updating user role with ID: {Id}", id);
        throw;
      }
    }

    public async Task RemoveRoleFromUserAsync(int userRoleId)
    {
      try
      {
        var userRole = await _context.UserRoles.FindAsync(userRoleId);
        if (userRole == null)
        {
          throw new KeyNotFoundException($"User role with ID {userRoleId} not found");
        }

        _context.UserRoles.Remove(userRole);
        await _context.SaveChangesAsync();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error removing role from user with ID: {Id}", userRoleId);
        throw;
      }
    }

    public async Task<bool> UserHasRoleAsync(string ldapUser, string roleName)
    {
      try
      {
        return await _context.UserRoles
            .AnyAsync(ur => ur.LdapUser == ldapUser &&
                      ur.RoleName.ToLower() == roleName.ToLower());
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error checking if user has role: {LdapUser}, {RoleName}", ldapUser, roleName);
        throw;
      }
    }

    public async Task<List<AvailableEmployeeViewModel>> GetEmployeesNotInRoleAsync(string roleName, string? department = null)
    {
      try
      {
        // Get all LDAP users who already have the specified role
        var existingRoleUsers = await _context.UserRoles
            .Where(r => r.RoleName.ToLower() == roleName.ToLower())
            .Select(r => r.LdapUser)
            .ToListAsync();

        // If department is specified, get employees from that department
        // If not, get all employees
        List<AvailableEmployeeViewModel> employees = new List<AvailableEmployeeViewModel>();

        using (SqlConnection connection = new SqlConnection(_sqlServerConnectionString))
        {
          await connection.OpenAsync();

          string query;
          SqlCommand command;

          if (!string.IsNullOrEmpty(department))
          {
            query = "SELECT * FROM SP_EMPLIST WHERE DEPARTMENT = @Department";
            command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@Department", department);
          }
          else
          {
            query = "SELECT * FROM SP_EMPLIST";
            command = new SqlCommand(query, connection);
          }

          using (SqlDataReader reader = await command.ExecuteReaderAsync())
          {
            while (await reader.ReadAsync())
            {
              employees.Add(new AvailableEmployeeViewModel
              {
                EmpId = reader["EMP_ID"]?.ToString() ?? string.Empty,
                Name = reader["NAME"]?.ToString() ?? string.Empty,
                Position = reader["POSITION_TITLE"]?.ToString() ?? string.Empty,
                Department = reader["DEPARTMENT"]?.ToString() ?? string.Empty,
                LdapUser = reader["LDAPUSER"]?.ToString() ?? string.Empty
              });
            }
          }
        }

        // Filter employees who are not already in the role
        return employees
            .Where(e => !string.IsNullOrEmpty(e.LdapUser) && !existingRoleUsers.Contains(e.LdapUser))
            .ToList();
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting employees not in role: {RoleName}, {Department}", roleName, department);
        throw;
      }
    }

    // Tambahkan method ini ke kelas RoleService.cs
    public async Task<List<string>> GetAllDepartmentsAsync()
    {
      try
      {
        var departments = new List<string>();

        using (SqlConnection connection = new SqlConnection(_sqlServerConnectionString))
        {
          await connection.OpenAsync();

          // Mengambil daftar departemen unik
          string query = "SELECT DISTINCT DEPARTMENT FROM SP_EMPLIST WHERE DEPARTMENT IS NOT NULL ORDER BY DEPARTMENT";
          using (SqlCommand command = new SqlCommand(query, connection))
          {
            command.CommandTimeout = 30; // Timeout yang wajar

            using (SqlDataReader reader = await command.ExecuteReaderAsync())
            {
              while (await reader.ReadAsync())
              {
                string department = reader["DEPARTMENT"]?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(department))
                {
                  departments.Add(department);
                }
              }
            }
          }
        }

        return departments;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error mengambil data departemen");
        throw;
      }
    }

    #endregion
  }
}
