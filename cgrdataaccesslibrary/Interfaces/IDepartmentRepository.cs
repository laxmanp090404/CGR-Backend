using cgrmodellibrary.Models;

namespace cgrdataaccesslibrary.Interfaces;

public interface IDepartmentRepository : IRepository<int, Department>
{
    Task<IEnumerable<Department>> GetAllAsync(bool? isActive);

    Task<Department?> GetDetailByIdAsync(int departmentId);

    Task<bool> HasEmployeesAsync(int departmentId);

    Task<bool> HasCategoriesAsync(int departmentId);
}