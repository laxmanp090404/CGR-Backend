using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.Complaint;

namespace cgrbussinesslogic.Interfaces;

public interface IComplaintService
{
    Task<ComplaintDto> CreateAsync(CreateComplaintDto dto, int employeeId);
    Task<ComplaintDto> GetByIdAsync(int complaintId, int currentEmployeeId, string role);
    Task<PagedResultDto<ComplaintDashboardDto>> GetPagedAsync(
        int page, int pageSize, int? statusId, int? priorityId, int? categoryId, int? departmentId, string? search,
        int currentEmployeeId, string role, int? currentDepartmentId);
    Task<IEnumerable<ComplaintHistoryDto>> GetHistoryAsync(int complaintId, int currentEmployeeId, string role);
    Task StartProgressAsync(int complaintId, int groId);
    Task ResolveAsync(int complaintId, int groId, ResolveComplaintDto dto);
    Task CloseAsync(int complaintId, int employeeId);
    Task ReopenAsync(int complaintId, int employeeId, ReopenComplaintDto dto);
    Task EscalateAsync(int complaintId, int currentEmployeeId, string role, EscalateComplaintDto dto);
}
