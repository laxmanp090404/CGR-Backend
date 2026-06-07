using cgrmodellibrary.Models;

namespace cgrdataaccesslibrary.Interfaces;

public interface IDepartmentRepository : IRepository<int, Department>
{
    Task<IEnumerable<Department>> GetAllAsync(bool? isActive);

    Task<Department?> GetDetailByIdAsync(int departmentId);

    Task<bool> HasEmployeesAsync(int departmentId);

    Task<bool> HasActiveCategoriesAsync(int departmentId);

    Task<bool> IsDepartmentHeadAssignedAsync(int employeeId,int? excludeDepartmentId = null);

    Task<bool> ExistsByNameAsync(string departmentName, int? excludeId = null);
    Task<Department?> GetByNameAsync(string departmentName);
}