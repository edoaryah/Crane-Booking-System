// Services/Employee/IEmployeeService.cs
using AspnetCoreMvcFull.Models;
using AspnetCoreMvcFull.Models.Auth;

namespace AspnetCoreMvcFull.Services
{
  public interface IEmployeeService
  {
    Task<IEnumerable<EmployeeDetails>> GetAllEmployeesAsync();
    Task<EmployeeDetails?> GetEmployeeByLdapUserAsync(string ldapUser);
    Task<EmployeeDetails?> GetManagerByDepartmentAsync(string department);
    Task<IEnumerable<EmployeeDetails>> GetPicCraneAsync();
  }
}
