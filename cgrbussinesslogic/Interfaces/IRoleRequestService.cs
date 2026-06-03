using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.RoleRequest;

namespace cgrbussinesslogic.Interfaces;

public interface IRoleRequestService
{
    // the employeeid and currentrole are coming from authenticated users ie jwt claims
    Task<RoleRequestDto> CreateAsync(CreateRoleRequestDto dto, int employeeId, string currentRole);

    Task<PagedResultDto<RoleRequestDto>> GetPagedAsync(int page, int pageSize, short? statusId);

    Task<IEnumerable<RoleRequestDto>> GetMyRequestsAsync(int employeeId);

    Task<RoleRequestDto> ApproveAsync(int roleRequestId, ApproveRoleRequestDto dto, int adminId);

    Task<RoleRequestDto> RejectAsync(int roleRequestId, string? remarks, int adminId);

    Task<RoleRequestDto> ManualRoleChangeAsync(int employeeId, ManualRoleChangeDto dto, int adminId);
}