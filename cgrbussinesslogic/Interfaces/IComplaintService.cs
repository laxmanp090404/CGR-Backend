using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.Complaint;

namespace cgrbussinesslogic.Interfaces;

public interface IComplaintService
{
    Task<ComplaintDto> CreateAsync(CreateComplaintDto dto);

    Task<ComplaintDto> GetByIdAsync(int complaintId);

    Task<PagedResultDto<ComplaintDashboardDto>> GetPagedAsync(
        int page, int pageSize, int? statusId, int? priorityId,
        int? categoryId, int? departmentId, string? search);
    Task<IEnumerable<ComplaintHistoryDto>> GetHistoryAsync(int complaintId);

    Task StartProgressAsync(int complaintId);

    Task ResolveAsync(int complaintId, ResolveComplaintDto dto);

    Task CloseAsync(int complaintId, CloseComplaintDto dto);

    Task ReopenAsync(int complaintId, ReopenComplaintDto dto);

    Task EscalateAsync(int complaintId, EscalateComplaintDto dto);

    Task AssignAsync(int complaintId, AssignComplaintDto dto);

    Task<IEnumerable<ComplaintAssignmentHistoryDto>> GetAssignmentHistoryAsync(int complaintId);

    Task<IEnumerable<ComplaintEscalationDto>> GetEscalationHistoryAsync(int complaintId);
}