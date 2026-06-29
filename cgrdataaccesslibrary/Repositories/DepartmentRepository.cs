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
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
    }

    public async Task<Department?> GetDetailByIdAsync(int departmentId)
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

    public async Task<Department?> GetByNameAsync(string departmentName)
    {
        return await _context.Departments
            .FirstOrDefaultAsync(
                d => d.DepartmentName.ToLower() ==
                departmentName.ToLower());
    }
    public async Task<bool> HasActiveCategoriesAsync(
        int departmentId)
    {
        return await _context.Categories
            .AnyAsync(
                c => c.DepartmentId == departmentId && c.IsActive);
    }

    public async Task<bool> ExistsByNameAsync(string departmentName, int? excludeDepartmentId = null)
    {
        var query = _context.Departments
            .Where(d =>
                d.DepartmentName.ToLower() ==
                departmentName.ToLower());
        if (excludeDepartmentId.HasValue)
        {
            query = query.Where(d => d.DepartmentId != excludeDepartmentId.Value);
        }
        return await query.AnyAsync();
    }
    // check if the employee is already a department head of any active department (excluding the current department)
    public async Task<bool> IsDepartmentHeadAssignedAsync(int employeeId,int? excludeDepartmentId = null)
    {
        var query =
            _context.Departments
                .Where(d =>
                    d.DepartmentHeadEmployeeId == employeeId &&
                    d.IsActive);

        if (excludeDepartmentId.HasValue)
        {
            query = query.Where(
                    d => d.DepartmentId != excludeDepartmentId.Value);
        }

        return await query.AnyAsync();
    }
}