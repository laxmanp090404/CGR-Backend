using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Repositories;

public class ComplaintCommentRepository : AbstractRepository<int, ComplaintComment>, IComplaintCommentRepository
{
    private readonly CGRContext _context;

    public ComplaintCommentRepository(CGRContext context) : base(context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ComplaintComment>> GetByComplaintIdAsync(int complaintId)
    {
        return await _context.ComplaintComments
            .Include(c => c.CommentedByNavigation)
            .Where(c => c.ComplaintId == complaintId)
            .OrderBy(c => c.CreatedAt)
            .ToListAsync();
    }
}
