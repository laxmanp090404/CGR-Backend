using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Repositories;

public class ComplaintAttachmentRepository : AbstractRepository<int, ComplaintAttachment>, IComplaintAttachmentRepository
{
    private readonly CGRContext _context;

    public ComplaintAttachmentRepository(CGRContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ComplaintAttachment>> GetByComplaintIdAsync(int complaintId)
    {
        return await _context.ComplaintAttachments
            .Where(a => a.ComplaintId == complaintId)
            .OrderBy(a => a.CreatedAt)
            .ToListAsync();
    }
}
