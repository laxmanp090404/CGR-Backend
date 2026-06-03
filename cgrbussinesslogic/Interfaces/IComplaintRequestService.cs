using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.ComplaintRequest;

namespace cgrbussinesslogic.Interfaces;

public interface IComplaintRequestService
{
    Task<ComplaintRequestDto> CreateAsync(int complaintId, CreateComplaintRequestDto dto, int employeeId);
    Task<PagedResultDto<ComplaintRequestDto>> GetPagedAsync(int page, int pageSize, short? requestTypeId, short? statusId);
    Task<ComplaintRequestDto> ReviewAsync(int requestId, ReviewComplaintRequestDto dto, int reviewerId, string reviewerRole);
}
