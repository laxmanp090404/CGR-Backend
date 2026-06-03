using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Context;
using cgrmodellibrary.DTOs.Analytics;
using Microsoft.EntityFrameworkCore;

namespace cgrbussinesslogic.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly CGRContext _context;

    public AnalyticsService(CGRContext context)
    {
        _context = context;
    }

    public async Task<ComplaintAnalyticsDto> GetComplaintAnalyticsAsync(
        int? departmentId, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.Complaints
            .Include(c => c.Category)
                .ThenInclude(cat => cat.Department)
            .Include(c => c.Status)
            .Include(c => c.Priority)
            .AsQueryable();

        if (departmentId.HasValue)
            query = query.Where(c => c.Category.DepartmentId == departmentId.Value);
        if (fromDate.HasValue)
            query = query.Where(c => c.CreatedAt >= fromDate.Value);
        if (toDate.HasValue)
            query = query.Where(c => c.CreatedAt <= toDate.Value);

        var complaints = await query.ToListAsync();

        var resolved = complaints.Where(c => c.ResolvedAt.HasValue).ToList();
        double avgResolutionHours = resolved.Any()
            ? resolved.Average(c => (c.ResolvedAt!.Value - c.CreatedAt).TotalHours)
            : 0;

        var slaBreached = await _context.VSlaBreachedComplaints.CountAsync(s =>
            (!departmentId.HasValue || s.CategoryId.HasValue) &&
            s.EscalationDueAt < DateTime.UtcNow);

        return new ComplaintAnalyticsDto
        {
            TotalComplaints = complaints.Count,
            OpenComplaints = complaints.Count(c => new short[] { 1, 2, 3, 4, 8 }.Contains(c.StatusId)),
            ResolvedComplaints = complaints.Count(c => c.StatusId == 5),
            ClosedComplaints = complaints.Count(c => c.StatusId == 6),
            RejectedComplaints = complaints.Count(c => c.StatusId == 7),
            EscalatedComplaints = complaints.Count(c => c.EscalationLevel > 0),
            SlaBreachedComplaints = slaBreached,
            AverageResolutionHours = Math.Round(avgResolutionHours, 2),
            ByCategory = complaints
                .GroupBy(c => new { c.CategoryId, Name = c.Category?.CategoryName ?? "Unknown" })
                .Select(g => new CategoryBreakdownDto
                {
                    CategoryId = g.Key.CategoryId,
                    CategoryName = g.Key.Name,
                    Count = g.Count()
                }).ToList(),
            ByDepartment = complaints
                .GroupBy(c => new
                {
                    DeptId = c.Category?.DepartmentId ?? 0,
                    Name = c.Category?.Department?.DepartmentName ?? "Unknown"
                })
                .Select(g => new DepartmentBreakdownDto
                {
                    DepartmentId = g.Key.DeptId,
                    DepartmentName = g.Key.Name,
                    Count = g.Count()
                }).ToList(),
            ByPriority = complaints
                .GroupBy(c => new { c.PriorityId, Name = c.Priority?.PriorityName ?? "Unknown" })
                .Select(g => new PriorityBreakdownDto
                {
                    PriorityId = g.Key.PriorityId,
                    PriorityName = g.Key.Name,
                    Count = g.Count()
                }).ToList()
        };
    }
}
