using cgrmodellibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Interfaces;

public interface IRoleRequestRepository : IRepository<int, RoleRequest>
{
    Task<RoleRequest?> GetPendingByEmployeeIdAsync(int employeeId);
    Task<(IEnumerable<RoleRequest> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, short? statusId);
    Task<IEnumerable<RoleRequest>> GetByEmployeeIdAsync(int employeeId);
}
