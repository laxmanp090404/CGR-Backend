using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.ComplaintRequest;

namespace cgrbussinesslogic.Interfaces;

public interface IComplaintRequestService
{
    Task<ComplaintRequestDto> CreateAsync(int complaintId,CreateComplaintRequestDto dto);

    Task<ComplaintRequestDto> ReviewAsync(int requestId,ReviewComplaintRequestDto dto);

    Task<PagedResultDto<ComplaintRequestDto>>GetPagedAsync(int page,int pageSize,short? statusId,int? requestedBy);
}
