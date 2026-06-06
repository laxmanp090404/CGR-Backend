using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;

namespace cgrdataaccesslibrary.Repositories;

public class DepartmentRepository
    : AbstractRepository<int, Department>,
      IDepartmentRepository
{
    private readonly CGRContext _context;

    public DepartmentRepository(CGRContext context)
        : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Department>> GetAllAsync(bool? isActive)
    {
        var query = _context.Departments
            .Include(d => d.DepartmentHeadEmployee)
            .AsQueryable();

        if (isActive.HasValue)
        {
            query = query.Where(
                d => d.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(d => d.DepartmentName)
            .ToListAsync();
    }

    public async Task<Department?> GetDetailByIdAsync(
        int departmentId)
    {
        return await _context.Departments
            .Include(d => d.DepartmentHeadEmployee)
            .Include(d => d.Categories)
            .FirstOrDefaultAsync(
                d => d.DepartmentId == departmentId);
    }

    public async Task<bool> HasEmployeesAsync(
        int departmentId)
    {
        return await _context.Employees
            .AnyAsync(
                e => e.DepartmentId == departmentId);
    }

    public async Task<bool> HasCategoriesAsync(
        int departmentId)
    {
        return await _context.Categories
            .AnyAsync(
                c => c.DepartmentId == departmentId);
    }
}