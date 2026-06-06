using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Repositories;

public class EmployeeRepository : AbstractRepository<int, Employee>, IEmployeeRepository
{
    private readonly CGRContext _context;

    public EmployeeRepository(CGRContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Employee?> GetByEmailAsync(string email)
    {
        return await _context.Employees
            .Include(e => e.Role)
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.Email == email && e.IsActive == true);
    }

    public async Task<bool> ExistsByEmailAsync(string email, int? excludeId = null)
    {
        if (excludeId.HasValue)
        {
            return await _context.Employees
                .AnyAsync(e => e.Email == email && e.EmployeeId != excludeId.Value);
        }
        return await _context.Employees
            .AnyAsync(e => e.Email == email);
    }

    public async Task<(IEnumerable<Employee> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? roleId, int? departmentId, string? search)
    {
        var query = _context.Employees
            .Include(e => e.Role)
            .Include(e => e.Department)
            .AsQueryable();

        if (roleId.HasValue)
        {
            query = query.Where(e => e.RoleId == roleId.Value);
        }
        if (departmentId.HasValue)
        {
            query = query.Where(e => e.DepartmentId == departmentId.Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e => e.EmployeeName.Contains(search) || e.Email.Contains(search));
        }

        int totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(e => e.EmployeeId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IEnumerable<VGroActiveWorkload>> GetGroActiveWorkloadAsync(int? departmentId)
    {
        var query = _context.VGroActiveWorkloads.AsQueryable();
        if (departmentId.HasValue)
        {
            query = query.Where(w => w.DepartmentId == departmentId.Value);
        }
        return await query.OrderBy(w => w.WeightedScore).ThenBy(w => w.EmployeeCreatedAt).ToListAsync();
    }
    public async Task<Employee?> GetByIdWithRoleAsync(int employeeId)
    {
        return await _context.Employees
            .Include(e => e.Role)
            .Include(e => e.Department)
            .FirstOrDefaultAsync(
                e => e.EmployeeId == employeeId
                     && e.IsActive);
    }
    public async Task<Employee?> GetDepartmentHeadAsync(int departmentId)
    {
        return await _context.Employees
            .Include(e => e.Role)
            .FirstOrDefaultAsync(
                e => e.DepartmentId == departmentId
                     && e.Role.RoleName == "DEPARTMENT_HEAD"
                     && e.IsActive);
    }
    public async Task<Employee?> GetAdminAsync()
    {
        return await _context.Employees
            .Include(e => e.Role)
            .FirstOrDefaultAsync(
                e => e.Role.RoleName == "ADMIN"
                     && e.IsActive);
    }
}
