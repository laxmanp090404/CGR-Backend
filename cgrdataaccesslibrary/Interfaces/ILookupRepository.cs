using cgrmodellibrary.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace cgrdataaccesslibrary.Interfaces;

public interface ILookupRepository
{
    Task<IEnumerable<Role>> GetRolesAsync();
    Task<IEnumerable<Priority>> GetPrioritiesAsync();
    Task<IEnumerable<ComplaintStatus>> GetComplaintStatusesAsync();
    Task<IEnumerable<RequestType>> GetRequestTypesAsync();
    Task<IEnumerable<NotificationType>> GetNotificationTypesAsync();
}
