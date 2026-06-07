using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Department;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;

namespace cgrbussinesslogic.Services;

public class DepartmentService : IDepartmentService
{
    private const short ROLE_DEPARTMENT_HEAD = 3;
    private const short ROLE_ADMIN = 4;

    private readonly IDepartmentRepository _departmentRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;

    public DepartmentService(
        IDepartmentRepository departmentRepository,
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService)
    {
        _departmentRepository = departmentRepository;
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
    }

    public async Task<IEnumerable<DepartmentDto>> GetAllAsync(bool? isActive)
    {
       
        var departments =
            await _departmentRepository.GetAllAsync(isActive);

        return departments.Select(MapToDto);
    }

    public async Task<DepartmentDto> GetByIdAsync(int departmentId)
    {

        var department =
            await _departmentRepository.GetDetailByIdAsync(departmentId)
            ?? throw new NotFoundException($"Department {departmentId} not found.");

        return MapToDto(department);
    }

    public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto)
    {
        ValidateAdmin();

        var existing =
            await _departmentRepository.GetByNameAsync(dto.DepartmentName);

        if (existing != null)
        {
            throw new ConflictException(
                $"Department '{dto.DepartmentName}' already exists.");
        }

        await ValidateDepartmentHeadAsync(
            dto.DepartmentHeadEmployeeId);

        var department = new Department
        {
            DepartmentName = dto.DepartmentName.Trim(),
            DepartmentHeadEmployeeId = dto.DepartmentHeadEmployeeId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var created =
            await _departmentRepository.Create(department);

        var reloaded =
            await _departmentRepository.GetDetailByIdAsync(
                created.DepartmentId)
            ?? throw new NotFoundException(
                "Unable to reload created department.");

        return MapToDto(reloaded);
    }

    public async Task<DepartmentDto> UpdateAsync(
        int departmentId,
        UpdateDepartmentDto dto)
    {
        ValidateAdmin();

        var department =
            await _departmentRepository.Get(departmentId)
            ?? throw new NotFoundException(
                $"Department {departmentId} not found.");

        var existing =
            await _departmentRepository.GetByNameAsync(
                dto.DepartmentName);

        if (existing != null &&
            existing.DepartmentId != departmentId)
        {
            throw new ConflictException(
                $"Department '{dto.DepartmentName}' already exists.");
        }

        if (!dto.IsActive)
        {
            var hasActiveCategories =
                await _departmentRepository
                    .HasActiveCategoriesAsync(departmentId);

            if (hasActiveCategories)
            {
                throw new BusinessRuleException(
                    "Cannot deactivate a department that contains active categories.");
            }
        }

        await ValidateDepartmentHeadAsync(
            dto.DepartmentHeadEmployeeId);

        department.DepartmentName =
            dto.DepartmentName.Trim();

        department.DepartmentHeadEmployeeId =
            dto.DepartmentHeadEmployeeId;

        department.IsActive =
            dto.IsActive;

        department.UpdatedAt =
            DateTime.UtcNow;

        await _departmentRepository.Update(
            department,
            departmentId);

        var reloaded =
            await _departmentRepository.GetDetailByIdAsync(
                departmentId)
            ?? throw new NotFoundException(
                $"Department {departmentId} not found.");

        return MapToDto(reloaded);
    }

    private async Task ValidateDepartmentHeadAsync(
        int? employeeId)
    {
        if (!employeeId.HasValue)
        {
            return;
        }

        var employee =
            await _employeeRepository.GetByIdWithRoleAsync(
                employeeId.Value)
            ?? throw new NotFoundException(
                $"Employee {employeeId.Value} not found.");

        if (!employee.IsActive)
        {
            throw new BusinessRuleException(
                "Department Head must be active.");
        }

        if (employee.RoleId != ROLE_DEPARTMENT_HEAD)
        {
            throw new BusinessRuleException(
                "Selected employee is not a Department Head.");
        }
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
                "Only admins can manage departments.");
        }
    }

    private static DepartmentDto MapToDto(
        Department department)
    {
        return new DepartmentDto
        {
            DepartmentId = department.DepartmentId,
            DepartmentName = department.DepartmentName,
            DepartmentHeadEmployeeId =
                department.DepartmentHeadEmployeeId,
            DepartmentHeadEmployeeName =
                department.DepartmentHeadEmployee?.EmployeeName,
            IsActive = department.IsActive,
            CreatedAt = department.CreatedAt
        };
    }
}