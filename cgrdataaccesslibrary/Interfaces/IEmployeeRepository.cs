using cgrmodellibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Interfaces;

public interface IEmployeeRepository : IRepository<int, Employee>
{
    Task<Employee?> GetByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email, int? excludeId = null);
    Task<(IEnumerable<Employee> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? roleId, int? departmentId, string? search);
    Task<IEnumerable<VGroActiveWorkload>> GetGroActiveWorkloadAsync(int? departmentId);
}
