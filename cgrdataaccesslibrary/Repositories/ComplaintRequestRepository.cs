using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Repositories;

public class ComplaintRequestRepository : AbstractRepository<int, ComplaintRequest>, IComplaintRequestRepository
{
    private readonly CGRContext _context;

    public ComplaintRequestRepository(CGRContext context) : base(context)
    {
        _context = context;
    }

    public async Task<ComplaintRequest?> GetPendingByComplaintAndTypeAsync(int complaintId, short requestTypeId)
    {
        return await _context.ComplaintRequests
            .FirstOrDefaultAsync(r => r.ComplaintId == complaintId 
                                   && r.RequestTypeId == requestTypeId 
                                   && r.RequestStatusId == 1); // 1 = PENDING
    }

    public async Task<(IEnumerable<ComplaintRequest> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, short? requestTypeId, short? statusId)
    {
        var query = _context.ComplaintRequests
            .Include(r => r.Complaint)
            .Include(r => r.RequestType)
            .Include(r => r.RequestStatus)
            .Include(r => r.RequestedByNavigation)
            .Include(r => r.ReviewedByNavigation)
            .AsQueryable();

        if (requestTypeId.HasValue)
        {
            query = query.Where(r => r.RequestTypeId == requestTypeId.Value);
        }
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
}
