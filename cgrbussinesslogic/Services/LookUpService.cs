using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.Models;

public class LookUpService : ILookUpService
{
    private readonly ILookupRepository _lookUpRepository;
    public LookUpService(ILookupRepository lookupRepository)
    {
        _lookUpRepository = lookupRepository;
    }
    public Task<IEnumerable<ComplaintStatus>> GetComplaintStatusesAsync()
    {
        return _lookUpRepository.GetComplaintStatusesAsync();
    }

    public Task<IEnumerable<NotificationType>> GetNotificationTypesAsync()
    {
        return _lookUpRepository.GetNotificationTypesAsync();
    }

    public Task<IEnumerable<Priority>> GetPrioritiesAsync()
    {
        return _lookUpRepository.GetPrioritiesAsync();
    }

    public Task<IEnumerable<RequestStatus>> GetRequestStatusesAsync()
    {
        return _lookUpRepository.GetRequestStatusesAsync();
    }

    public Task<IEnumerable<RequestType>> GetRequestTypesAsync()
    {
        return _lookUpRepository.GetRequestTypesAsync();
    }

    public Task<IEnumerable<Role>> GetRolesAsync()
    {
        return _lookUpRepository.GetRolesAsync();
    }
    
}