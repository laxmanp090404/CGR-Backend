using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.Employee;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;

namespace cgrbussinesslogic.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;
    private const short ROLE_ADMIN = 4;


    public EmployeeService(IEmployeeRepository employeeRepository, ICurrentUserService currentUserService)
    {
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
    }

   public async Task<EmployeeDto> GetByIdAsync(int employeeId)
{
    if (!_currentUserService.IsAuthenticated)
    {
        throw new UnauthorizedAccessException();
    }

    if (_currentUserService.RoleId != ROLE_ADMIN &&
        _currentUserService.EmployeeId != employeeId)
    {
        throw new ForbiddenException("You can only view your own profile.");
    }

    var employee =
        await _employeeRepository.GetDetailByIdAsync(
            employeeId)
        ?? throw new NotFoundException(
            $"Employee {employeeId} not found.");

    return MapToDto(employee);
}
    public async Task<PagedResultDto<EmployeeDto>> GetPagedAsync(
      int page,
      int pageSize,
      bool? isActive,
      int? roleId,
      int? departmentId,
      string? search)
    {
        ValidateAdmin();

        var (items, totalCount) =
            await _employeeRepository.GetPagedAsync(
                page,
                pageSize,
                roleId,
                departmentId,
                search, isActive);


        return new PagedResultDto<EmployeeDto>
        {
            Items =
                items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }
    public async Task<EmployeeDto> UpdateAsync(int employeeId,UpdateEmployeeDto dto)
{
    if (!_currentUserService.IsAuthenticated)
    {
        throw new UnauthorizedAccessException();
    }

    if (_currentUserService.RoleId != ROLE_ADMIN &&
        _currentUserService.EmployeeId != employeeId)
    {
        throw new ForbiddenException("You can only update your own profile  or Admin can only do this.");
    }

    var employee =
        await _employeeRepository.Get(
            employeeId);

    if (await _employeeRepository.ExistsByEmailAsync(
            dto.Email,
            employeeId))
    {
        throw new ConflictException(
            $"Email '{dto.Email}' already exists.");
    }

    employee.EmployeeName =
        dto.EmployeeName.Trim();

    employee.Email =
        dto.Email.Trim();

    employee.MobileNumber =
        dto.MobileNumber.Trim();

    employee.UpdatedAt =
        DateTime.UtcNow;

    await _employeeRepository.Update(
        employee,
        employeeId);

    var reloaded =
        await _employeeRepository.GetDetailByIdAsync(
            employeeId)
        ?? throw new NotFoundException(
            $"Employee {employeeId} not found.");

    return MapToDto(reloaded);
}

    public async Task DeactivateAsync(int employeeId)
    {
        ValidateAdmin();

        var employee =
            await _employeeRepository.Get(
                employeeId);

        if (employee.RoleId == ROLE_ADMIN)
        {
            throw new BusinessRuleException(
                "Admin cannot be deactivated.");
        }

        if (await _employeeRepository.IsDepartmentHeadAsync(
                employeeId))
        {
            throw new BusinessRuleException(
                "Department Head cannot be deactivated.");
        }

        if (await _employeeRepository.HasActiveAssignedComplaintsAsync(
                employeeId))
        {
            throw new BusinessRuleException(
                "Employee has active complaint assignments.");
        }

        employee.IsActive = false;
        employee.UpdatedAt = DateTime.UtcNow;

        await _employeeRepository.Update(
            employee,
            employeeId);
    }
    public async Task RestoreAsync(int employeeId)
    {
        ValidateAdmin();

        var employee =
            await _employeeRepository.Get(
                employeeId);

        employee.IsActive = true;
        employee.UpdatedAt = DateTime.UtcNow;

        await _employeeRepository.Update(
            employee,
            employeeId);
    }


    private static EmployeeDto MapToDto(Employee employee)
    {
        return new EmployeeDto
        {
            EmployeeId = employee.EmployeeId,
            EmployeeName = employee.EmployeeName,
            Email = employee.Email,
            MobileNumber = employee.MobileNumber,
            RoleId = employee.RoleId,
            RoleName = employee.Role?.RoleName ?? string.Empty,
            DepartmentId = employee.DepartmentId,
            DepartmentName =
                employee.Department?.DepartmentName,
            IsActive = employee.IsActive,
            CreatedAt = employee.CreatedAt
        };
    }

    private void ValidateAdmin()
    {
        if (!_currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException();
        }

        if (_currentUserService.RoleId != ROLE_ADMIN)
        {
            throw new ForbiddenException(
                "Only admins can perform this action.");
        }
    }
}
