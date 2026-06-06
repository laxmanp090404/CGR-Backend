using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Repositories;

public class ComplaintRepository : AbstractRepository<int, Complaint>, IComplaintRepository
{
    private readonly CGRContext _context;

    public ComplaintRepository(CGRContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Complaint?> GetDetailByIdAsync(int id)
    {
        return await _context.Complaints
            .Include(c => c.Status)
            .Include(c => c.Priority)
            .Include(c => c.Category)
                .ThenInclude(cat => cat.Department)
            .Include(c => c.RaisedByEmployee)
            .Include(c => c.CurrentHandlerEmployee)
            .Include(c=>c.ComplaintAttachments)
            .FirstOrDefaultAsync(c => c.ComplaintId == id);
    }

    public async Task<(IEnumerable<VComplaintDashboard> Items, int TotalCount)> GetPagedDashboardAsync(
        int page, int pageSize, int? statusId, int? priorityId, int? categoryId, int? departmentId, string? search,
        int employeeIdFilter, string roleFilter, int? deptIdFilter)
    {
        var query = _context.VComplaintDashboards
            .Join(_context.Complaints,
                  v => v.ComplaintId,
                  c => c.ComplaintId,
                  (v, c) => new { View = v, Complaint = c });

        if (roleFilter == "EMPLOYEE")
        {
            query = query.Where(x => x.Complaint.RaisedByEmployeeId == employeeIdFilter);
        }
        else if (roleFilter == "GRO")
        {
            query = query.Where(x => x.Complaint.RaisedByEmployeeId == employeeIdFilter 
                                  || x.Complaint.CurrentHandlerEmployeeId == employeeIdFilter);
        }
        else if (roleFilter == "DEPARTMENT_HEAD")
        {
            query = query.Where(x => x.Complaint.Category.DepartmentId == deptIdFilter 
                                  || x.Complaint.RaisedByEmployeeId == employeeIdFilter);
        }
        // ADMIN sees all

        // Filter parameters
        if (statusId.HasValue)
        {
            query = query.Where(x => x.Complaint.StatusId == statusId.Value);
        }
        if (priorityId.HasValue)
        {
            query = query.Where(x => x.Complaint.PriorityId == priorityId.Value);
        }
        if (categoryId.HasValue)
        {
            query = query.Where(x => x.Complaint.CategoryId == categoryId.Value);
        }
        if (departmentId.HasValue)
        {
            query = query.Where(x => x.Complaint.Category.DepartmentId == departmentId.Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.View.ComplaintTitle!.Contains(search) 
                                  || x.View.RaisedByName!.Contains(search) 
                                  || x.View.CurrentHandlerName!.Contains(search));
        }

        int totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(x => x.Complaint.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.View)
            .ToListAsync();

        return (items, totalCount);
    }
}
