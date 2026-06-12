using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Analytics;
using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;
namespace cgrdataaccesslibrary.Repositories;

public class AnalyticsRepository : IAnalyticsRepository
{
    private readonly CGRContext _context;

    public AnalyticsRepository(CGRContext context)
    {
        _context = context;
    }

    #region StatusIds

    private const short STATUS_SUBMITTED = 1;
    private const short STATUS_ASSIGNED = 2;
    private const short STATUS_IN_PROGRESS = 3;
    private const short STATUS_ESCALATED = 4;
    private const short STATUS_RESOLVED = 5;
    private const short STATUS_CLOSED = 6;
    private const short STATUS_REJECTED = 7;
    private const short STATUS_REOPENED = 8;
    private const short STATUS_EXTERNALLY_ESCALATED = 9;

    #endregion
    private const short REQUEST_STATUS_PENDING = 1;

    #region RoleIds
    private const short ROLE_EMPLOYEE = 1;
    private const short ROLE_GRO = 2;
    private const short ROLE_DEPARTMENT_HEAD = 3;
    private const short ROLE_ADMIN = 4;
    #endregion
    private static readonly short[] OpenStatuses =
    [
        STATUS_SUBMITTED,
        STATUS_ASSIGNED,
        STATUS_IN_PROGRESS,
        STATUS_ESCALATED,
        STATUS_REOPENED
    ];

    private async Task<IEnumerable<StatusDistributionDto>> GetStatusDistributionForComplaintsAsync(IQueryable<Complaint> complaints)
    {
        var counts = await complaints
            .GroupBy(c => c.StatusId)
            .Select(g => new
            {
                StatusId = g.Key,
                Count = g.Count()
            })
            .ToDictionaryAsync(x => x.StatusId, x => x.Count);

        var statuses = await _context.ComplaintStatuses
            .AsNoTracking()
            .OrderBy(s => s.StatusId)
            .ToListAsync();

        return statuses.Select(s => new StatusDistributionDto
        {
            StatusId = s.StatusId,
            StatusName = s.StatusName,
            Count = counts.TryGetValue(s.StatusId, out var count) ? count : 0
        }).ToList();
    }
    //dashboard for admin
    public async Task<AdminDashboardDto> GetAdminDashboardAsync()
{
    var complaints = _context.Complaints.AsNoTracking().AsQueryable();

    var totalComplaints = await complaints.CountAsync();

    var openComplaints = await complaints.CountAsync(c => OpenStatuses.Contains(c.StatusId));

    var resolvedComplaints = await complaints.CountAsync(c => c.StatusId == STATUS_RESOLVED);

    var closedComplaints = await complaints.CountAsync(c => c.StatusId == STATUS_CLOSED);

    var rejectedComplaints = await complaints.CountAsync(c => c.StatusId == STATUS_REJECTED);

    var externallyEscalated = await complaints.CountAsync(c => c.StatusId == STATUS_EXTERNALLY_ESCALATED);

    var overdueComplaints = await complaints.CountAsync(c =>
        c.EscalationDueAt != null &&
        c.EscalationDueAt < DateTime.UtcNow &&
        !c.Status.IsTerminal);

    var pendingComplaintRequests = await _context.ComplaintRequests
        .AsNoTracking()
        .CountAsync(r => r.RequestStatusId == REQUEST_STATUS_PENDING);

    var pendingRoleRequests = await _context.RoleRequests
        .AsNoTracking()
        .CountAsync(r => r.RequestStatusId == REQUEST_STATUS_PENDING);

    var activeEmployees = await _context.Employees
        .AsNoTracking()
        .CountAsync(e => e.IsActive);

    var activeDepartments = await _context.Departments
        .AsNoTracking()
        .CountAsync(d => d.IsActive);

    var resolutionHours = await complaints
        .Where(c =>
            (c.StatusId == STATUS_RESOLVED ||
             c.StatusId == STATUS_CLOSED) &&
            c.ResolvedAt != null)
        .Select(c => (double)(c.ResolvedAt!.Value - c.CreatedAt).TotalHours)
        .ToListAsync();

    double? avgResolutionHours = resolutionHours.Count == 0
        ? null
        : Math.Round(resolutionHours.Average(), 1);

    var resolved = await complaints
        .Where(c =>
            (c.StatusId == STATUS_RESOLVED ||
             c.StatusId == STATUS_CLOSED) &&
            c.ResolvedAt != null &&
            c.EscalationDueAt != null)
        .ToListAsync();

    double? slaCompliancePercent = null;

    if (resolved.Count > 0)
    {
        var withinSla = resolved.Count(c => c.ResolvedAt <= c.EscalationDueAt);

        slaCompliancePercent = Math.Round(withinSla * 100.0 / resolved.Count, 1);
    }

    return new AdminDashboardDto
    {
        TotalComplaints = totalComplaints,
        OpenComplaints = openComplaints,
        ResolvedComplaints = resolvedComplaints,
        ClosedComplaints = closedComplaints,
        RejectedComplaints = rejectedComplaints,
        ExternallyEscalatedComplaints = externallyEscalated,
        PendingComplaintRequests = pendingComplaintRequests,
        PendingRoleRequests = pendingRoleRequests,
        ActiveEmployees = activeEmployees,
        ActiveDepartments = activeDepartments,
        OverdueComplaints = overdueComplaints,
        AvgResolutionHours = avgResolutionHours,
        SlaCompliancePercent = slaCompliancePercent
    };
}
    // dashboard for complaint raiser (can include GRO , Department head and Employee can raise a complaint)
    public async Task<MyDashboardDto> GetMyDashboardAsync(int employeeId)
{
    var complaints = _context.Complaints
        .AsNoTracking()
        .Where(c => c.RaisedByEmployeeId == employeeId);

    var resolutionHours = await complaints
        .Where(c =>
            (c.StatusId == STATUS_RESOLVED ||
             c.StatusId == STATUS_CLOSED) &&
            c.ResolvedAt != null)
        .Select(c => (double)(c.ResolvedAt!.Value - c.CreatedAt).TotalHours)
        .ToListAsync();

    var statusBreakdown = await GetStatusDistributionForComplaintsAsync(complaints);

    return new MyDashboardDto
    {
        TotalRaised = await complaints.CountAsync(),

        OpenComplaints = await complaints.CountAsync(c =>
            OpenStatuses.Contains(c.StatusId)),

        ResolvedComplaints = await complaints.CountAsync(c =>
            c.StatusId == STATUS_RESOLVED),

        ClosedComplaints = await complaints.CountAsync(c =>
            c.StatusId == STATUS_CLOSED),

        RejectedComplaints = await complaints.CountAsync(c =>
            c.StatusId == STATUS_REJECTED),

        ExternallyEscalatedComplaints = await complaints.CountAsync(c =>
            c.StatusId == STATUS_EXTERNALLY_ESCALATED),

        AvgResolutionHours = resolutionHours.Count == 0
            ? null
            : Math.Round(resolutionHours.Average(), 1),

        StatusBreakdown = statusBreakdown
    };
}
public async Task<GroDashboardDto> GetGroDashboardAsync(int employeeId)
{
    var complaints = _context.Complaints
        .AsNoTracking()
        .Where(c => c.CurrentHandlerEmployeeId == employeeId);

    var resolutionHours = await complaints
        .Where(c =>
            (c.StatusId == STATUS_RESOLVED ||
             c.StatusId == STATUS_CLOSED) &&
            c.ResolvedAt != null)
        .Select(c => (double)(c.ResolvedAt!.Value - c.CreatedAt).TotalHours)
        .ToListAsync();

    return new GroDashboardDto
    {
        AssignedToMe = await complaints.CountAsync(c => c.StatusId == STATUS_ASSIGNED),

        InProgressByMe = await complaints.CountAsync(c => c.StatusId == STATUS_IN_PROGRESS),

        ResolvedByMe = await complaints.CountAsync(c =>
            c.StatusId == STATUS_RESOLVED ||
            c.StatusId == STATUS_CLOSED),

        EscalatedByMe = await complaints.CountAsync(c => c.StatusId == STATUS_ESCALATED),

        OverdueAssignedToMe = await complaints.CountAsync(c =>
            c.EscalationDueAt != null &&
            c.EscalationDueAt < DateTime.UtcNow &&
            !c.Status.IsTerminal),

        AvgResolutionHours = resolutionHours.Count == 0
            ? null
            : Math.Round(resolutionHours.Average(), 1)
    };
}
  // for admin : all depts for department head : their dept
    public async Task<IEnumerable<DepartmentDashboardDto>> GetDepartmentDashboardsAsync(int? departmentId, bool isAdmin)
{
    var departments = _context.Departments
        .AsNoTracking()
        .Where(d => d.IsActive);

    if (!isAdmin && departmentId.HasValue)
    {
        departments = departments.Where(d => d.DepartmentId == departmentId.Value);
    }

    var result = new List<DepartmentDashboardDto>();

    foreach (var department in await departments.ToListAsync())
    {
        var complaints = _context.Complaints
            .AsNoTracking()
            .Where(c => c.Category.DepartmentId == department.DepartmentId);

        var totalResolved = await complaints.CountAsync(c =>
            c.StatusId == STATUS_RESOLVED ||
            c.StatusId == STATUS_CLOSED);

        var resolvedWithinSla = await complaints.CountAsync(c =>
            (c.StatusId == STATUS_RESOLVED ||
             c.StatusId == STATUS_CLOSED) &&
            c.ResolvedAt != null &&
            c.EscalationDueAt != null &&
            c.ResolvedAt <= c.EscalationDueAt);

        double? slaCompliance = null;

        if (totalResolved > 0)
        {
            slaCompliance = Math.Round(resolvedWithinSla * 100.0 / totalResolved, 1);
        }

        var resolutionHours = await complaints
            .Where(c =>
                (c.StatusId == STATUS_RESOLVED ||
                 c.StatusId == STATUS_CLOSED) &&
                c.ResolvedAt != null)
            .Select(c => (double)(c.ResolvedAt!.Value - c.CreatedAt).TotalHours)
            .ToListAsync();

        var statusDistribution = await GetStatusDistributionForComplaintsAsync(complaints);

        result.Add(new DepartmentDashboardDto
        {
            DepartmentId = department.DepartmentId,
            DepartmentName = department.DepartmentName,
            TotalComplaints = await complaints.CountAsync(),
            OpenComplaints = await complaints.CountAsync(c => OpenStatuses.Contains(c.StatusId)),
            OverdueComplaints = await complaints.CountAsync(c =>
                c.EscalationDueAt != null &&
                c.EscalationDueAt < DateTime.UtcNow &&
                !c.Status.IsTerminal),
            AvgResolutionHours = resolutionHours.Count == 0
                ? null
                : Math.Round(resolutionHours.Average(), 1),
            SlaCompliancePercent = slaCompliance,
            StatusDistribution = statusDistribution
        });
    }

    return result;
}
 public async Task<IEnumerable<StatusDistributionDto>> GetStatusDistributionAsync(short roleId, int employeeId, int? departmentId)
{
    var complaints = _context.Complaints.AsNoTracking().AsQueryable();

    switch (roleId)
    {
        case ROLE_EMPLOYEE:
            complaints = complaints.Where(c => c.RaisedByEmployeeId == employeeId);
            break;

        case ROLE_GRO:
            complaints = complaints.Where(c => c.CurrentHandlerEmployeeId == employeeId);
            break;

        case ROLE_DEPARTMENT_HEAD:
            complaints = complaints.Where(c => c.Category.DepartmentId == departmentId);
            break;

        case ROLE_ADMIN:
            break;
    }

    return await GetStatusDistributionForComplaintsAsync(complaints);
}
   
public async Task<IEnumerable<TopCategoryDto>> GetTopCategoriesAsync(short roleId, int? departmentId, int n)
{
    var complaints = _context.Complaints.AsNoTracking().AsQueryable();

    if (roleId == ROLE_DEPARTMENT_HEAD)
    {
        complaints = complaints.Where(c => c.Category.DepartmentId == departmentId);
    }

    return await complaints
        .GroupBy(c => new
        {
            c.CategoryId,
            c.Category.CategoryName,
            DepartmentName = c.Category.Department.DepartmentName
        })
        .Select(g => new TopCategoryDto
        {
            CategoryId = g.Key.CategoryId,
            CategoryName = g.Key.CategoryName,
            DepartmentName = g.Key.DepartmentName,
            ComplaintCount = g.Count()
        })
        .OrderByDescending(x => x.ComplaintCount)
        .Take(n)
        .ToListAsync();
}
public async Task<ComplaintAnalyticsDto> GetComplaintAnalyticsAsync(short roleId, int? departmentId)
{
    var complaints = _context.Complaints
        .AsNoTracking()
        .AsQueryable();

    if (roleId == ROLE_DEPARTMENT_HEAD)
    {
        complaints = complaints.Where(c =>
            c.Category.DepartmentId == departmentId);
    }

    var totalComplaints = await complaints.CountAsync();

    var escalatedComplaintIds = await _context.ComplaintEscalations
        .AsNoTracking()
        .Where(e =>
            roleId != ROLE_DEPARTMENT_HEAD ||
            e.Complaint.Category.DepartmentId == departmentId)
        .Select(e => e.ComplaintId)
        .Distinct()
        .ToListAsync();

    var slaBreachedComplaintsCount = await complaints.CountAsync(c =>
        c.EscalationDueAt != null &&
        ((c.ResolvedAt == null && !c.Status.IsTerminal && c.EscalationDueAt < DateTime.UtcNow) ||
         (c.ResolvedAt != null && (c.StatusId == STATUS_RESOLVED || c.StatusId == STATUS_CLOSED) && c.ResolvedAt > c.EscalationDueAt)));

    var resolutionHours = await complaints
        .Where(c =>
            (c.StatusId == STATUS_RESOLVED ||
             c.StatusId == STATUS_CLOSED) &&
            c.ResolvedAt != null)
        .Select(c =>
            (double)(c.ResolvedAt!.Value - c.CreatedAt).TotalHours)
        .ToListAsync();

    var byCategory = await complaints
        .GroupBy(c => new
        {
            c.CategoryId,
            c.Category.CategoryName
        })
        .Select(g => new CategoryBreakdownDto
        {
            CategoryId = g.Key.CategoryId,
            CategoryName = g.Key.CategoryName,
            Count = g.Count()
        })
        .OrderByDescending(x => x.Count)
        .ToListAsync();

    var byDepartment = await complaints
        .GroupBy(c => new
        {
            c.Category.DepartmentId,
            c.Category.Department.DepartmentName
        })
        .Select(g => new DepartmentBreakdownDto
        {
            DepartmentId = g.Key.DepartmentId,
            DepartmentName = g.Key.DepartmentName,
            Count = g.Count()
        })
        .OrderByDescending(x => x.Count)
        .ToListAsync();

    var byPriority = await complaints
        .GroupBy(c => new
        {
            c.PriorityId,
            c.Priority.PriorityName
        })
        .Select(g => new PriorityBreakdownDto
        {
            PriorityId = g.Key.PriorityId,
            PriorityName = g.Key.PriorityName,
            Count = g.Count()
        })
        .OrderByDescending(x => x.Count)
        .ToListAsync();

    return new ComplaintAnalyticsDto
    {
        TotalComplaints = totalComplaints,
        EscalatedComplaints = escalatedComplaintIds.Count,
        SlaBreachedComplaints = slaBreachedComplaintsCount,
        AverageResolutionHours = resolutionHours.Count == 0
            ? null
            : Math.Round(resolutionHours.Average(), 1),
        ByCategory = byCategory,
        ByDepartment = byDepartment,
        ByPriority = byPriority
    };
}
}