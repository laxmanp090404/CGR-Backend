using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Context;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Common;
using cgrmodellibrary.DTOs.RoleRequest;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;

namespace cgrbussinesslogic.Services;

public class RoleRequestService : IRoleRequestService
{
    private const short ROLE_EMPLOYEE = 1;
    private const short ROLE_GRO = 2;
    private const short ROLE_DEPT_HEAD = 3;
    private const short ROLE_ADMIN = 4;

    private const short REQUEST_STATUS_PENDING = 1;
    private const short REQUEST_STATUS_APPROVED = 2;
    private const short REQUEST_STATUS_REJECTED = 3;

    private const short NOTIF_ROLE_REQUEST_OUTCOME = 10;

    private readonly IRoleRequestRepository _roleRequestRepository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly IRepository<int, Department> _departmentRepository;
    private readonly CGRContext _context;
    public RoleRequestService(IRoleRequestRepository roleRequestRepository, IEmployeeRepository employeeRepository, IRepository<int, Department> departmentRepository, CGRContext context)
    {
        _roleRequestRepository = roleRequestRepository;
        _employeeRepository = employeeRepository;
        _departmentRepository = departmentRepository;
        _context = context;
    }
    // helpers
    private static short GetRoleId(string role)
    {
        switch (role)
        {
            case "EMPLOYEE":
                return ROLE_EMPLOYEE;

            case "GRO":
                return ROLE_GRO;

            case "DEPARTMENT_HEAD":
                return ROLE_DEPT_HEAD;

            case "ADMIN":
                return ROLE_ADMIN;

            default:
                throw new ValidationException("Invalid role.");
        }
    }
    private static bool IsValidRequestUpgrade(short currentRole, short requestedRole)
    {
        return (currentRole == ROLE_EMPLOYEE && requestedRole == ROLE_GRO) ||
               (currentRole == ROLE_GRO && requestedRole == ROLE_DEPT_HEAD);
    }
    public async Task<RoleRequestDto> CreateAsync(CreateRoleRequestDto dto, int employeeId, string currentRole)
    {

        short currentRoleId = GetRoleId(currentRole);

        if (!IsValidRequestUpgrade(currentRoleId, dto.RequestedRoleId))
            throw new BusinessRuleException("Invalid role upgrade path. Only EMPLOYEE to GRO or GRO to DEPARTMENT_HEAD upgrades are allowed.");


        var existing = await _roleRequestRepository.GetPendingByEmployeeIdAsync(employeeId);
        if (existing != null)
            throw new ConflictException("You already have a pending role upgrade request.");

        var request = new RoleRequest
        {
            EmployeeId = employeeId,
            CurrentRoleId = currentRoleId,
            RequestedRoleId = dto.RequestedRoleId,
            RequestStatusId = REQUEST_STATUS_PENDING,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _roleRequestRepository.Create(request);
        var reloaded = await _roleRequestRepository.GetByEmployeeIdAsync(employeeId);
        return MapToDto(reloaded.First(r => r.RoleRequestId == created.RoleRequestId));
    }

    public async Task<RoleRequestDto> RejectAsync(int roleRequestId, string? remarks, int adminId)
    {
        var request = await _roleRequestRepository.Get(roleRequestId);

        if (request.RequestStatusId != REQUEST_STATUS_PENDING)
        {
            throw new BusinessRuleException(
                "This request has already been reviewed.");
        }

        request.RequestStatusId = REQUEST_STATUS_REJECTED;

        request.ReviewedBy = adminId;
        request.ReviewedAt = DateTime.UtcNow;
        request.Remarks = remarks;

        await _roleRequestRepository.Update(request, roleRequestId);

        var requests = await _roleRequestRepository.GetByEmployeeIdAsync(request.EmployeeId);

        return MapToDto(requests.First(r => r.RoleRequestId == roleRequestId));
    }
    public async Task<PagedResultDto<RoleRequestDto>> GetPagedAsync(int page, int pageSize, short? statusId)
    {
        var (items, total) = await _roleRequestRepository.GetPagedAsync(page, pageSize, statusId);
        return new PagedResultDto<RoleRequestDto>
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<IEnumerable<RoleRequestDto>> GetMyRequestsAsync(int employeeId)
    {
        var requests = await _roleRequestRepository.GetByEmployeeIdAsync(employeeId);
        return requests.Select(MapToDto);
    }
    private static RoleRequestDto MapToDto(RoleRequest r) => new()
    {
        RoleRequestId = r.RoleRequestId,
        EmployeeId = r.EmployeeId,
        EmployeeName = r.Employee?.EmployeeName ?? string.Empty,
        CurrentRoleId = r.CurrentRoleId,
        CurrentRoleName = r.CurrentRole?.RoleName ?? string.Empty,
        RequestedRoleId = r.RequestedRoleId,
        RequestedRoleName = r.RequestedRole?.RoleName ?? string.Empty,
        RequestStatusId = r.RequestStatusId,
        RequestStatusName = r.RequestStatus?.StatusName ?? string.Empty,
        ReviewedBy = r.ReviewedBy,
        ReviewedByName = r.ReviewedByNavigation?.EmployeeName,
        Remarks = r.Remarks,
        CreatedAt = r.CreatedAt,
        ReviewedAt = r.ReviewedAt
    };

    public async Task<RoleRequestDto> ApproveAsync(int roleRequestId, ApproveRoleRequestDto dto, int adminId)
    {
        using var dbTxn = await _context.Database.BeginTransactionAsync();

        try
        {
            var request = await _roleRequestRepository.Get(roleRequestId);

            if (request.RequestStatusId != REQUEST_STATUS_PENDING)
            {
                throw new BusinessRuleException("This request has already been reviewed.");
            }

            var employee = await _employeeRepository.Get(request.EmployeeId);

            int? finalDepartmentId = employee.DepartmentId;

            if (!finalDepartmentId.HasValue)
            {
                if (!dto.AssignDepartmentId.HasValue)
                {
                    throw new ValidationException("Department assignment is required.");
                }

                finalDepartmentId = dto.AssignDepartmentId.Value;
            }

            var department = await _departmentRepository.Get(finalDepartmentId.Value);

            if (request.RequestedRoleId == ROLE_DEPT_HEAD)
            {
                if (department.DepartmentHeadEmployeeId.HasValue &&
                    department.DepartmentHeadEmployeeId != employee.EmployeeId)
                {
                    throw new BusinessRuleException("Department already has a department head.");
                }

                var allDepartments = await _departmentRepository.GetAll();

                if (allDepartments.Any(d => d.DepartmentHeadEmployeeId == employee.EmployeeId &&
                                            d.DepartmentId != department.DepartmentId))
                {
                    throw new BusinessRuleException("Employee is already department head of another department.");
                }
            }



            employee.RoleId = request.RequestedRoleId;
            employee.DepartmentId = finalDepartmentId;
            employee.UpdatedAt = DateTime.UtcNow;

            await _employeeRepository.Update(employee, employee.EmployeeId);



            if (request.RequestedRoleId == ROLE_DEPT_HEAD)
            {
                department.DepartmentHeadEmployeeId = employee.EmployeeId;

                await _departmentRepository.Update(department, department.DepartmentId);
            }

            request.RequestStatusId = REQUEST_STATUS_APPROVED;
            request.ReviewedBy = adminId;
            request.ReviewedAt = DateTime.UtcNow;
            request.Remarks = dto.Remarks;

            await _roleRequestRepository.Update(request, roleRequestId);

            await dbTxn.CommitAsync();

            var requests = await _roleRequestRepository.GetByEmployeeIdAsync(employee.EmployeeId);

            return MapToDto(requests.First(r => r.RoleRequestId == roleRequestId));
        }
        catch
        {
            await dbTxn.RollbackAsync();
            throw;
        }
    }


    public async Task<RoleRequestDto> ManualRoleChangeAsync(int employeeId, ManualRoleChangeDto dto, int adminId)
    {
        using var dbTxn = await _context.Database.BeginTransactionAsync();

        try
        {
            var employee = await _employeeRepository.Get(employeeId);

            if (employee.RoleId == ROLE_ADMIN)
            {
                throw new BusinessRuleException("Admin role cannot be changed.");
            }

            if (dto.TargetRoleId == ROLE_ADMIN)
            {
                throw new BusinessRuleException("Cannot assign ADMIN role.");
            }

            if (employee.RoleId == dto.TargetRoleId)
            {
                throw new ValidationException("Employee already has this role.");
            }

            if (dto.TargetRoleId != ROLE_EMPLOYEE &&
                dto.TargetRoleId != ROLE_GRO &&
                dto.TargetRoleId != ROLE_DEPT_HEAD)
            {
                throw new ValidationException("Invalid target role.");
            }

            int? finalDepartmentId = employee.DepartmentId;

            if (!finalDepartmentId.HasValue && dto.DepartmentId.HasValue)
            {
                finalDepartmentId = dto.DepartmentId;
            }

            if ((dto.TargetRoleId == ROLE_GRO || dto.TargetRoleId == ROLE_DEPT_HEAD) &&
                !finalDepartmentId.HasValue)
            {
                throw new ValidationException("Department assignment is required.");
            }

            Department? department = null;

            if (finalDepartmentId.HasValue)
            {
                department = await _departmentRepository.Get(finalDepartmentId.Value);
            }



            if (dto.TargetRoleId == ROLE_DEPT_HEAD)
            {
                if (department == null)
                {
                    throw new ValidationException("Department assignment is required.");
                }

                if (department.DepartmentHeadEmployeeId.HasValue &&
                    department.DepartmentHeadEmployeeId != employee.EmployeeId)
                {
                    throw new BusinessRuleException("Department already has a department head.");
                }

                var allDepartments = await _departmentRepository.GetAll();

                if (allDepartments.Any(d => d.DepartmentHeadEmployeeId == employee.EmployeeId &&
                                            d.DepartmentId != department.DepartmentId))
                {
                    throw new BusinessRuleException("Employee is already department head of another department.");
                }
            }

            if (employee.RoleId == ROLE_DEPT_HEAD &&
                dto.TargetRoleId != ROLE_DEPT_HEAD)
            {
                var allDepartments = await _departmentRepository.GetAll();

                var headedDepartment = allDepartments.FirstOrDefault(d =>
                    d.DepartmentHeadEmployeeId == employee.EmployeeId);

                if (headedDepartment != null)
                {
                    headedDepartment.DepartmentHeadEmployeeId = null;

                    await _departmentRepository.Update(headedDepartment, headedDepartment.DepartmentId);
                }
            }

            short oldRoleId = employee.RoleId;



            employee.RoleId = dto.TargetRoleId;
            employee.DepartmentId = finalDepartmentId;
            employee.UpdatedAt = DateTime.UtcNow;

            await _employeeRepository.Update(employee, employee.EmployeeId);



            if (dto.TargetRoleId == ROLE_DEPT_HEAD)
            {
                department!.DepartmentHeadEmployeeId = employee.EmployeeId;

                await _departmentRepository.Update(department, department.DepartmentId);
            }

            var roleRequest = new RoleRequest
            {
                EmployeeId = employee.EmployeeId,
                CurrentRoleId = oldRoleId,
                RequestedRoleId = dto.TargetRoleId,
                RequestStatusId = REQUEST_STATUS_APPROVED,
                ReviewedBy = adminId,
                ReviewedAt = DateTime.UtcNow,
                Remarks = dto.Remarks,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _roleRequestRepository.Create(roleRequest);

            await dbTxn.CommitAsync();

            var requests = await _roleRequestRepository.GetByEmployeeIdAsync(employee.EmployeeId);

            return MapToDto(requests.First(r => r.RoleRequestId == created.RoleRequestId));
        }
        catch
        {
            await dbTxn.RollbackAsync();
            throw;
        }
    }
}
