using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Employee;
using cgrmodellibrary.Exceptions;

namespace cgrbussinesslogic.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;

    public EmployeeService(IEmployeeRepository employeeRepository)
    {
        _employeeRepository = employeeRepository;
    }

    public async Task<EmployeeDto> GetByIdAsync(int id)
    {
        var emp = await _employeeRepository.Get(id);
        return MapToDto(emp);
    }

    public async Task<(IEnumerable<EmployeeDto> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize, int? roleId, int? departmentId, string? search)
    {
        var (items, total) = await _employeeRepository.GetPagedAsync(page, pageSize, roleId, departmentId, search);
        return (items.Select(MapToDto), total);
    }

    public async Task<EmployeeDto> UpdateAsync(int id, UpdateEmployeeDto dto, int currentUserId, string currentRole)
    {
        var emp = await _employeeRepository.Get(id);

        // Only admin or the employee themselves can update
        if (currentRole != "ADMIN" && currentUserId != id)
        {
            throw new ForbiddenException("You are not allowed to update this employee.");
        }

        if (!string.IsNullOrWhiteSpace(dto.EmployeeName))
            emp.EmployeeName = dto.EmployeeName;
        if (!string.IsNullOrWhiteSpace(dto.MobileNumber))
            emp.MobileNumber = dto.MobileNumber;

        emp.UpdatedAt = DateTime.UtcNow;
        var updated = await _employeeRepository.Update(emp, id);
        return MapToDto(updated);
    }

    public async Task DeactivateAsync(int id, int currentUserId, string currentRole)
    {
        if (currentRole != "ADMIN")
            throw new ForbiddenException("Only Admin can deactivate employees.");

        var emp = await _employeeRepository.Get(id);
        if (!emp.IsActive)
            throw new BusinessRuleException("Employee is already inactive.");

        emp.IsActive = false;
        emp.UpdatedAt = DateTime.UtcNow;
        await _employeeRepository.Update(emp, id);
    }

    public async Task ReactivateAsync(int id, int currentUserId)
    {
        var emp = await _employeeRepository.Get(id);
        if (emp.IsActive)
            throw new BusinessRuleException("Employee is already active.");

        emp.IsActive = true;
        emp.UpdatedAt = DateTime.UtcNow;
        await _employeeRepository.Update(emp, id);
    }

   
    private static EmployeeDto MapToDto(cgrmodellibrary.Models.Employee emp) => new()
    {
        EmployeeId = emp.EmployeeId,
        EmployeeName = emp.EmployeeName,
        Email = emp.Email,
        MobileNumber = emp.MobileNumber,
        RoleId = emp.RoleId,
        RoleName = emp.Role?.RoleName ?? string.Empty,
        DepartmentId = emp.DepartmentId,
        DepartmentName = emp.Department?.DepartmentName,
        IsActive = emp.IsActive,
        CreatedAt = emp.CreatedAt
    };
}
