using cgrmodellibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Interfaces;

public interface IComplaintAssignmentRepository : IRepository<int, ComplaintAssignmentHistory>
{
    Task<List<int>> GetPreviousHandlersAsync(int complaintId);
}
