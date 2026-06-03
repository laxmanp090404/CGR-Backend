using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Repositories;

public class ComplaintHistoryRepository : AbstractRepository<int, ComplaintHistory>, IComplaintHistoryRepository
{
    private readonly CGRContext _context;

    public ComplaintHistoryRepository(CGRContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ComplaintHistory>> GetByComplaintIdAsync(int complaintId)
    {
        return await _context.ComplaintHistories
            .Include(h => h.OldStatus)
            .Include(h => h.NewStatus)
            .Include(h => h.OldHandlerEmployee)
            .Include(h => h.NewHandlerEmployee)
            .Include(h => h.ChangedByNavigation)
            .Where(h => h.ComplaintId == complaintId)
            .OrderBy(h => h.CreatedAt)
            .ToListAsync();
    }
}
