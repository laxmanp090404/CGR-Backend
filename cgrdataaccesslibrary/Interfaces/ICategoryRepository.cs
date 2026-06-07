using cgrmodellibrary.Models;

namespace cgrdataaccesslibrary.Interfaces;

public interface ICategoryRepository : IRepository<int, Category>
{
    Task<IEnumerable<Category>> GetAllAsync(bool? isActive);

    Task<IEnumerable<Category>> GetByDepartmentAsync(
        int? departmentId,
        bool? isActive);

    Task<Category?> GetByNameAsync(string categoryName);

    Task<Category?> GetDetailByIdAsync(int categoryId);
}