using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.Employee;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cgrbussinesslogic.Services;

public class EmployeeService : IEmployeeService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly CGRContext _context;
    private const short ROLE_ADMIN = 4;


    public EmployeeService(
        IEmployeeRepository employeeRepository,
        ICurrentUserService currentUserService,
        CGRContext context)
    {
        _employeeRepository = employeeRepository;
        _currentUserService = currentUserService;
        _context = context;
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

    public async Task<IEnumerable<GroActiveWorkloadDto>> GetGroActiveWorkloadAsync(int? departmentId)
    {
        if (!_currentUserService.IsAuthenticated)
        {
            throw new UnauthorizedAccessException();
        }

        string role = _currentUserService.Role;
        int? filterDepartmentId = null;

        if (role == "DEPARTMENT_HEAD")
        {
            filterDepartmentId = _currentUserService.DepartmentId;
        }
        else if (role == "ADMIN")
        {
            filterDepartmentId = departmentId;
        }
        else
        {
            throw new ForbiddenException("Only Admin or Department Heads can view GRO workload.");
        }

        var workloads = await _employeeRepository.GetGroActiveWorkloadAsync(filterDepartmentId);
        var departments = await _context.Departments.ToDictionaryAsync(d => d.DepartmentId, d => d.DepartmentName);
        var groIds = workloads.Select(w => w.EmployeeId).Where(id => id.HasValue).Select(id => id!.Value).ToList();

        // Active Statuses (excluding Resolved 5, Closed 6, Rejected 7, Externally Escalated 9)
        var terminalStatusIds = new short[] { 5, 6, 7, 9 };

        var complaintCounts = await _context.Complaints
            .Where(c => c.CurrentHandlerEmployeeId.HasValue 
                     && groIds.Contains(c.CurrentHandlerEmployeeId.Value) 
                     && !terminalStatusIds.Contains(c.StatusId))
            .GroupBy(c => new { c.CurrentHandlerEmployeeId, c.PriorityId })
            .Select(g => new {
                EmployeeId = g.Key.CurrentHandlerEmployeeId!.Value,
                PriorityId = g.Key.PriorityId,
                Count = g.Count()
            })
            .ToListAsync();

        var result = new List<GroActiveWorkloadDto>();
        foreach (var w in workloads)
        {
            if (!w.EmployeeId.HasValue) continue;

            int empId = w.EmployeeId.Value;
            var countsForGro = complaintCounts.Where(c => c.EmployeeId == empId).ToList();

            var deptName = w.DepartmentId.HasValue && departments.TryGetValue(w.DepartmentId.Value, out var name)
                ? name
                : "Unknown";

            result.Add(new GroActiveWorkloadDto
            {
                EmployeeId = empId,
                EmployeeName = w.EmployeeName ?? "Unknown",
                DepartmentId = w.DepartmentId ?? 0,
                DepartmentName = deptName,
                ActiveComplaintCount = (int)(w.ActiveComplaintCount ?? 0),
                WeightedScore = w.WeightedScore ?? 0,
                LowCount = countsForGro.FirstOrDefault(c => c.PriorityId == 1)?.Count ?? 0,
                MediumCount = countsForGro.FirstOrDefault(c => c.PriorityId == 2)?.Count ?? 0,
                HighCount = countsForGro.FirstOrDefault(c => c.PriorityId == 3)?.Count ?? 0,
                CriticalCount = countsForGro.FirstOrDefault(c => c.PriorityId == 4)?.Count ?? 0
            });
        }

        return result;
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
