using cgrmodellibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Interfaces;

public interface IComplaintRequestRepository : IRepository<int, ComplaintRequest>
{
    Task<ComplaintRequest?> GetPendingByComplaintAndTypeAsync(int complaintId, short requestTypeId);
    Task<(IEnumerable<ComplaintRequest> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, short? requestTypeId, short? statusId);
    Task<ComplaintRequest?> GetDetailAsync(int requestId);
}
