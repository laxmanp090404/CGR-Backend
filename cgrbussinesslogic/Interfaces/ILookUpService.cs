using cgrmodellibrary.Models;

namespace cgrbussinesslogic.Interfaces;
public interface ILookUpService
{
    Task<IEnumerable<Role>> GetRolesAsync();
    Task<IEnumerable<Priority>> GetPrioritiesAsync();
    Task<IEnumerable<ComplaintStatus>> GetComplaintStatusesAsync();
    Task<IEnumerable<RequestType>> GetRequestTypesAsync();
    Task<IEnumerable<NotificationType>> GetNotificationTypesAsync();
    Task<IEnumerable<RequestStatus>> GetRequestStatusesAsync();
}