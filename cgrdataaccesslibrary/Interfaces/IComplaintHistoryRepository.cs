using cgrmodellibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Interfaces;

public interface IComplaintHistoryRepository : IRepository<int, ComplaintHistory>
{
    Task<IEnumerable<ComplaintHistory>> GetByComplaintIdAsync(int complaintId);
}
