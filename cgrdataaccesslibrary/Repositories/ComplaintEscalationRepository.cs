using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrdataaccesslibrary.Repositories;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;

public class ComplaintEscalationRepository : AbstractRepository<int, ComplaintEscalation>, IComplaintEscalationRepository
{
    private readonly CGRContext _context;
    public ComplaintEscalationRepository(CGRContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ComplaintEscalation>> GetByComplaintIdAsync(int complaintId)
    {
        return await _context.ComplaintEscalations
            .Where(e => e.ComplaintId == complaintId)
            .Include(e=>e.EscalationRule)
            .Include(e=>e.EscalatedByEmployee)
            .Include(e=>e.EscalatedToEmployee)
            .OrderBy(e => e.EscalatedAt)
            .ToListAsync();
    }
}