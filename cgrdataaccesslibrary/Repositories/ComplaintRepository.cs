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
    private const short STATUS_CLOSED = 6;
    private const short STATUS_REJECTED = 7;
    private const short STATUS_EXTERNALLY_ESCALATED = 9;
    private const short STATUS_RESOLVED = 5;
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
            .Include(c => c.ComplaintAttachments)
            .FirstOrDefaultAsync(c => c.ComplaintId == id);
    }

    public async Task<(IEnumerable<VComplaintDashboard> Items, int TotalCount)> GetPagedDashboardAsync(
        int page, int pageSize, int? statusId, int? priorityId, int? categoryId, int? departmentId, string? search,
        int employeeIdFilter, string roleFilter, int? deptIdFilter,bool? raisedbyMe)
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
        if (raisedbyMe.HasValue && raisedbyMe.Value)
        {
            query = query.Where(x => x.Complaint.RaisedByEmployeeId == employeeIdFilter);
        }
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
   var searchText = search.Trim().ToLower();

query = query.Where(x =>
   
        (x.View.ComplaintTitle != null &&
         x.View.ComplaintTitle.ToLower().Contains(searchText))
        ||
        (x.View.RaisedByName != null &&
         x.View.RaisedByName.ToLower().Contains(searchText))
        ||
        (x.View.CurrentHandlerName != null &&
         x.View.CurrentHandlerName.ToLower().Contains(searchText))
    );
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

    // get complaints that expired above escalationdueat
    public async Task<IEnumerable<Complaint>> GetEscalationDueComplaintsAsync()
    {
        return await _context.Complaints
            .Include(c => c.Category)
            .Include(c => c.Priority)
            .Include(c => c.CurrentHandlerEmployee)
            .Where(c =>
                c.EscalationDueAt <= DateTime.UtcNow &&
                c.StatusId != STATUS_CLOSED &&
                c.StatusId != STATUS_REJECTED &&
                c.StatusId != STATUS_EXTERNALLY_ESCALATED &&
                c.StatusId != STATUS_RESOLVED)
            .ToListAsync();
    }

    public async Task<bool> ExistsRecentDuplicateAsync(int raisedByEmployeeId, string title, string description, TimeSpan window)
    {
        var threshold = DateTime.UtcNow.Subtract(window);
        return await _context.Complaints.AnyAsync(c =>
            c.RaisedByEmployeeId == raisedByEmployeeId &&
            c.CreatedAt >= threshold &&
            c.ComplaintTitle.Trim().ToLower() == title.Trim().ToLower() &&
            c.ComplaintDescription.Trim().ToLower() == description.Trim().ToLower());
    }
    public async Task<(IEnumerable<Complaint> Items, int TotalCount)> GetMyWorkQueueAsync(
        int page,
        int pageSize,
        int employeeId,
        int? statusId = null,
        int? priorityId = null,
        int? categoryId = null,
        int? departmentId = null,
        string? search = null)
    {
        var query = _context.Complaints
            .Include(c => c.Status)
            .Include(c => c.Priority)
            .Include(c => c.Category).ThenInclude(cat => cat.Department)
            .Include(c => c.RaisedByEmployee)
            .Include(c => c.CurrentHandlerEmployee)
            .Where(c => c.CurrentHandlerEmployeeId == employeeId &&
                        (c.StatusId == 2 || c.StatusId == 3 || c.StatusId == 4))
            .AsQueryable();

        if (statusId.HasValue)
        {
            query = query.Where(c => c.StatusId == statusId.Value);
        }
        if (priorityId.HasValue)
        {
            query = query.Where(c => c.PriorityId == priorityId.Value);
        }
        if (categoryId.HasValue)
        {
            query = query.Where(c => c.CategoryId == categoryId.Value);
        }
        if (departmentId.HasValue)
        {
            query = query.Where(c => c.Category.DepartmentId == departmentId.Value);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(c => c.ComplaintTitle.ToLower().Contains(searchLower) ||
                                     c.ComplaintDescription.ToLower().Contains(searchLower) ||
                                     c.RaisedByEmployee.EmployeeName.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderBy(c => c.EscalationDueAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
