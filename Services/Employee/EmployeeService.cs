// Services/Employee/EmployeeService.cs
using Microsoft.Data.SqlClient;
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Models.Auth;

namespace AspnetCoreMvcFull.Services
{
  public class EmployeeService : IEmployeeService
  {
    private readonly string _connectionString;
    private readonly ILogger<EmployeeService> _logger;

    public EmployeeService(IConfiguration configuration, ILogger<EmployeeService> logger)
    {
      _connectionString = configuration.GetConnectionString("SqlServerConnection")
          ?? throw new InvalidOperationException("SQL Server connection string 'SqlServerConnection' not found");
      _logger = logger;
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

    public async Task<IEnumerable<EmployeeDetails>> GetPicCraneAsync()
    {
      var picEmployees = new List<EmployeeDetails>();

      try
      {
        using (var connection = new SqlConnection(_connectionString))
        {
          await connection.OpenAsync();

          string query = "SELECT * FROM SP_EMPLIST WHERE DEPARTMENT = 'Stores & Inventory Control' AND POSITION_LVL = 'SUPV_LVL' AND EMP_STATUS = 'KPC'";
          using (var command = new SqlCommand(query, connection))
          {
            using (var reader = await command.ExecuteReaderAsync())
            {
              while (await reader.ReadAsync())
              {
                picEmployees.Add(MapEmployeeFromReader(reader));
              }
            }
          }
        }

        return picEmployees;
      }
      catch (Exception ex)
      {
        _logger.LogError(ex, "Error fetching PIC Crane employees");
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
