using cgrmodellibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Interfaces;

public interface ICategoryRepository : IRepository<int, Category>
{
    Task<IEnumerable<Category>> GetByDepartmentAsync(int? departmentId, bool? isActive);
}
