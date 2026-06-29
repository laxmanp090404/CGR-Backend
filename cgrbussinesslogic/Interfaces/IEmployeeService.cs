using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.Employee;

namespace cgrbussinesslogic.Interfaces;

public interface IEmployeeService
{
    Task<PagedResultDto<EmployeeDto>> GetPagedAsync(
        int page,
        int pageSize,
        bool? isActive,
        int? roleId,
        int? departmentId,
        string? search);

    Task<EmployeeDto> GetByIdAsync(int employeeId);

    Task<EmployeeDto> UpdateAsync(int employeeId,UpdateEmployeeDto dto);

    Task DeactivateAsync(int employeeId);

    Task RestoreAsync(int employeeId);

    Task<IEnumerable<GroActiveWorkloadDto>> GetGroActiveWorkloadAsync(int? departmentId);
}