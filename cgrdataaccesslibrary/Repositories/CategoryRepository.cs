using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Repositories;

public class CategoryRepository : AbstractRepository<int, Category>, ICategoryRepository
{
    private readonly CGRContext _context;

    public CategoryRepository(CGRContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Category>> GetByDepartmentAsync(int? departmentId, bool? isActive)
    {
        var query = _context.Categories
            .Include(c => c.Department)
            .Include(c => c.DefaultPriority)
            .AsQueryable();

        if (departmentId.HasValue)
            query = query.Where(c => c.DepartmentId == departmentId.Value);

        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);

        return await query.OrderBy(c => c.CategoryName).ToListAsync();
    }
}
