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
    private short STATUS_CLOSED = 6;
    private short STATUS_REJECTED = 7;
    private short STATUS_EXTERNALLY_ESCALATED = 9;

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
        int page, int pageSize, int? roleId, int? departmentId, string? search,bool? isActive)
    {
        var query = _context.Employees
            .Include(e => e.Role)
            .Include(e => e.Department)
            .AsQueryable();
        if (isActive.HasValue)
        {
            query = query.Where(e => e.IsActive == isActive.Value);
        }
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
            query = query.Where(e => e.EmployeeName.Trim().ToLower().Contains(search.ToLower()) || e.Email.Trim().ToLower().Contains(search.ToLower()));
        }

        int totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(e => e.EmployeeId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
    // method to get active workload of gros for assignment engine
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
    // get employees with details for admin view
    public async Task<IEnumerable<Employee>> GetAllDetailedAsync(bool? isActive)
    {
        var query =
            _context.Employees
                .Include(e => e.Role)
                .Include(e => e.Department)
                .AsQueryable();

        if (isActive.HasValue)
        {
            query =
                query.Where(
                    e => e.IsActive == isActive.Value);
        }

        return await query
            .OrderBy(e => e.EmployeeName)
            .ToListAsync();
    }
    // get employee details by id for admin view and individual employee view
    public async Task<Employee?> GetDetailByIdAsync(int employeeId)
    {
        return await _context.Employees
            .Include(e => e.Role)
            .Include(e => e.Department)
            .FirstOrDefaultAsync(
                e => e.EmployeeId == employeeId);
    }
    // finds if active complaint exist
    public async Task<bool> HasActiveAssignedComplaintsAsync(int employeeId)
    {
        return await _context.Complaints.AnyAsync(
            c =>
                c.CurrentHandlerEmployeeId == employeeId &&
                c.StatusId != STATUS_CLOSED &&
                c.StatusId != STATUS_REJECTED &&
                c.StatusId != STATUS_EXTERNALLY_ESCALATED);
    }
    // check if employee is  dept head
    public async Task<bool> IsDepartmentHeadAsync(int employeeId)
{
    return await _context.Departments.AnyAsync(
        d =>
            d.DepartmentHeadEmployeeId == employeeId &&
            d.IsActive);
}
}
