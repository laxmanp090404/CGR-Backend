using cgrmodellibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Interfaces;

public interface IComplaintCommentRepository : IRepository<int, ComplaintComment>
{
    Task<IEnumerable<ComplaintComment>> GetByComplaintIdAsync(int complaintId);
    Task<bool> ExistsRecentDuplicateAsync(int commentedByEmployeeId, string message, TimeSpan window);
}
