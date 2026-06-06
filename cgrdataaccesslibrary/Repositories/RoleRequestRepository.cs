using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Repositories;

public class RoleRequestRepository : AbstractRepository<int, RoleRequest>, IRoleRequestRepository
{
    private readonly CGRContext _context;

    public RoleRequestRepository(CGRContext context) : base(context)
    {
        _context = context;
    }

    public async Task<RoleRequest?> GetPendingByEmployeeIdAsync(int employeeId)
    {
        return await _context.RoleRequests
            .FirstOrDefaultAsync(r => r.EmployeeId == employeeId && r.RequestStatusId == 1); // 1 = PENDING
    }

    public async Task<(IEnumerable<RoleRequest> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, short? statusId)
    {
        var query = _context.RoleRequests
            .Include(r => r.Employee)
            .Include(r => r.CurrentRole)
            .Include(r => r.RequestedRole)
            .Include(r => r.RequestStatus)
            .AsQueryable();

        if (statusId.HasValue)
        {
            query = query.Where(r => r.RequestStatusId == statusId.Value);
        }

        int totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IEnumerable<RoleRequest>> GetByEmployeeIdAsync(int employeeId)
    {
        return await _context.RoleRequests
            .Include(r => r.CurrentRole)
            .Include(r => r.RequestedRole)
            .Include(r => r.RequestStatus)
            .Include(r => r.ReviewedByNavigation)
            .Where(r => r.EmployeeId == employeeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }
}
