using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Repositories;

public class ComplaintAssignmentRepository : AbstractRepository<int, ComplaintAssignmentHistory>, IComplaintAssignmentRepository
{
    private readonly CGRContext _context;

    public ComplaintAssignmentRepository(CGRContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<int>> GetPreviousHandlersAsync(int complaintId)
    {
        return await _context.ComplaintAssignmentHistories
            .Where(h => h.ComplaintId == complaintId)
            .Select(h => h.NewHandlerEmployeeId)
            .Distinct()
            .ToListAsync();
    }
    public async Task<IEnumerable<ComplaintAssignmentHistory>> GetByComplaintIdAsync(int complaintId)
{
    return await _context.ComplaintAssignmentHistories
        .Include(h => h.OldHandlerEmployee)
        .Include(h => h.NewHandlerEmployee)
        .Include(h => h.AssignedByNavigation)
        .Where(h => h.ComplaintId == complaintId)
        .OrderBy(h => h.AssignedAt)
        .ToListAsync();
}
}
