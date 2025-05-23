// Services/Employee/EmployeeService.cs
using Microsoft.Data.SqlClient;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Models.Auth;
using AspnetCoreMvcFull.Models.Role;
using AspnetCoreMvcFull.Services.Role;

namespace AspnetCoreMvcFull.Services
{
  public class EmployeeService : IEmployeeService
  {
    private readonly string _connectionString;
    private readonly ILogger<EmployeeService> _logger;
    private readonly IRoleService _roleService; // ✅ Tambahkan dependency

    public EmployeeService(IConfiguration configuration, ILogger<EmployeeService> logger, IRoleService roleService)
    {
      _connectionString = configuration.GetConnectionString("SqlServerConnection")
          ?? throw new InvalidOperationException("SQL Server connection string 'SqlServerConnection' not found");
      _logger = logger;
      _roleService = roleService; // ✅ Assign dependency
    }

    public async Task<IEnumerable<EmployeeDetails>> GetAllEmployeesAsync()
    {
      var employees = new List<EmployeeDetails>();

      try
      {
        using (var connection = new SqlConnection(_connectionString))
        {
          await connection.OpenAsync();

          string query = "SELECT * FROM SP_EMPLIST WHERE EMP_STATUS = 'KPC'";
          using (var command = new SqlCommand(query, connection))
          {
            using (var reader = await command.ExecuteReaderAsync())
            {
              while (await reader.ReadAsync())
              {
                employees.Add(MapEmployeeFromReader(reader));
              }
            }
          }
        }

        return employees;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error fetching all employees");
        throw;
      }
    }

    public async Task<EmployeeDetails?> GetEmployeeByLdapUserAsync(string ldapUser)
    {
      try
      {
        using (var connection = new SqlConnection(_connectionString))
        {
          await connection.OpenAsync();

          string query = "SELECT * FROM SP_EMPLIST WHERE LDAPUSER = @LdapUser AND EMP_STATUS = 'KPC'";
          using (var command = new SqlCommand(query, connection))
          {
            command.Parameters.AddWithValue("@LdapUser", ldapUser);

            using (var reader = await command.ExecuteReaderAsync())
            {
              if (await reader.ReadAsync())
              {
                return MapEmployeeFromReader(reader);
              }
            }
          }
        }

        return null;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error fetching employee with LDAP user: {LdapUser}", ldapUser);
        throw;
      }
    }

    public async Task<EmployeeDetails?> GetManagerByDepartmentAsync(string department)
    {
      try
      {
        using (var connection = new SqlConnection(_connectionString))
        {
          await connection.OpenAsync();

          string query = "SELECT * FROM SP_EMPLIST WHERE DEPARTMENT = @Department AND POSITION_LVL = 'MGR_LVL' AND EMP_STATUS = 'KPC'";
          using (var command = new SqlCommand(query, connection))
          {
            command.Parameters.AddWithValue("@Department", department);

            using (var reader = await command.ExecuteReaderAsync())
            {
              if (await reader.ReadAsync())
              {
                return MapEmployeeFromReader(reader);
              }
            }
          }
        }

        _logger.LogWarning("No manager found for department: {Department}", department);
        return null;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error fetching manager for department: {Department}", department);
        throw;
      }
    }

    // public async Task<IEnumerable<EmployeeDetails>> GetPicCraneAsync()
    // {
    //   var picEmployees = new List<EmployeeDetails>();

    //   try
    //   {
    //     using (var connection = new SqlConnection(_connectionString))
    //     {
    //       await connection.OpenAsync();

    //       string query = "SELECT * FROM SP_EMPLIST WHERE DEPARTMENT = 'Stores & Inventory Control' AND POSITION_LVL = 'SUPV_LVL' AND EMP_STATUS = 'KPC'";
    //       using (var command = new SqlCommand(query, connection))
    //       {
    //         using (var reader = await command.ExecuteReaderAsync())
    //         {
    //           while (await reader.ReadAsync())
    //           {
    //             picEmployees.Add(MapEmployeeFromReader(reader));
    //           }
    //         }
    //       }
    //     }

    //     return picEmployees;
    //   }
    //   catch (Exception ex)
    //   {
    //     _logger.LogError(ex, "Error fetching PIC Crane employees");
    //     throw;
    //   }
    // }

    // Di Services/Employee/EmployeeService.cs
    public async Task<IEnumerable<EmployeeDetails>> GetPicCraneAsync()
    {
      var picEmployees = new List<EmployeeDetails>();

      try
      {
        // ❌ HAPUS cara lama ini:
        // string query = "SELECT * FROM SP_EMPLIST WHERE DEPARTMENT = 'Stores & Inventory Control' AND POSITION_LVL = 'SUPV_LVL' AND EMP_STATUS = 'KPC'";

        // ✅ GUNAKAN cara baru dengan RoleService:
        // Dapatkan semua user dengan role PIC
        var picUsers = await _roleService.GetUsersByRoleNameAsync(Roles.PIC);

        foreach (var picUser in picUsers)
        {
          if (!string.IsNullOrEmpty(picUser.LdapUser))
          {
            // Dapatkan detail employee berdasarkan LDAP user
            var employee = await GetEmployeeByLdapUserAsync(picUser.LdapUser);
            if (employee != null && employee.EmpStatus == "KPC") // Pastikan masih aktif
            {
              picEmployees.Add(employee);
            }
          }
        }

        return picEmployees;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error fetching PIC Crane employees using role management");
        throw;
      }
    }

    private EmployeeDetails MapEmployeeFromReader(SqlDataReader reader)
    {
      return new EmployeeDetails
      {
        EmpId = reader["EMP_ID"]?.ToString() ?? string.Empty,
        Name = reader["NAME"]?.ToString() ?? string.Empty,
        PositionTitle = reader["POSITION_TITLE"]?.ToString() ?? string.Empty,
        Division = reader["DIVISION"]?.ToString() ?? string.Empty,
        Department = reader["DEPARTMENT"]?.ToString() ?? string.Empty,
        Email = reader["EMAIL"]?.ToString() ?? string.Empty,
        PositionLvl = reader["POSITION_LVL"]?.ToString() ?? string.Empty,
        LdapUser = reader["LDAPUSER"]?.ToString() ?? string.Empty,
        EmpStatus = reader["EMP_STATUS"]?.ToString() ?? string.Empty
      };
    }
  }
}
