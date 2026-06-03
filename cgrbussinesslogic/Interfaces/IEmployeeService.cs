using cgrmodellibrary.DTOs.Employee;

namespace cgrbussinesslogic.Interfaces;

public interface IEmployeeService
{
    Task<EmployeeDto> GetByIdAsync(int id);
    Task<(IEnumerable<EmployeeDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? roleId, int? departmentId, string? search);
    Task<EmployeeDto> UpdateAsync(int id, UpdateEmployeeDto dto, int currentUserId, string currentRole);
    Task DeactivateAsync(int id, int currentUserId, string currentRole);
    Task ReactivateAsync(int id, int currentUserId);
}
