using cgrbussinesslogic.Interfaces;
using cgrdataaccesslibrary.Interfaces;
using cgrmodellibrary.DTOs.Auth;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Models;
using BCrypt.Net;
using System.Transactions;

namespace cgrbussinesslogic.Services;

public class AuthService : IAuthService
{
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ITokenService _tokenService;

    private readonly IRoleRequestRepository _roleRequestRepository;
    private readonly IRepository<int, Department> _departmentRepository;
    private short EMPLOYEE_ROLE_ID = 1;
    private short GRO_ROLE_ID = 2;
    private short ROLE_REQUEST_PENDING_STATUS_ID = 1;
    public AuthService(IEmployeeRepository employeeRepository, ITokenService tokenService, IRoleRequestRepository roleRequestRepository, IRepository<int, Department> departmentRepository)
    {
        _employeeRepository = employeeRepository;
        _tokenService = tokenService;
        _roleRequestRepository = roleRequestRepository;
        _departmentRepository = departmentRepository;
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var employee = await _employeeRepository.GetByEmailAsync(dto.Email);
        // employee not there
        if (employee == null)
        {
            throw new ValidationException("Invalid email or password and account may not exist.");
        }
        // deactivated employee
        if (employee.IsActive == false)
        {
            throw new BusinessRuleException("Account is deactivated. Please contact an administrator.");
        }
        // invalid credentials
        if (!BCrypt.Net.BCrypt.Verify(dto.Password, employee.PasswordHash))
        {
            throw new ValidationException("Invalid email or password.");
        }

        var token = _tokenService.GenerateToken(employee);
        return new LoginResponseDto
        {
            Token = token,
            EmployeeName = employee.EmployeeName,
            Role = employee.Role.RoleName,
            EmployeeId = employee.EmployeeId
        };
    }

    public async Task<LoginResponseDto> RegisterAsync(RegisterRequestDto dto)
    {
        var exists = await _employeeRepository.ExistsByEmailAsync(dto.Email);
        // in register no other go so tell that account already exists
        if (exists)
        {
            throw new ConflictException("An account with this email already exists.");
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12);
        // whatever role requested until approved it is in employee role only
       

        var employee = new Employee
        {
            EmployeeName = dto.EmployeeName,
            Email = dto.Email,
            MobileNumber = dto.MobileNumber,
            PasswordHash = passwordHash,
            RoleId = EMPLOYEE_ROLE_ID,
            DepartmentId = dto.DepartmentId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        if (dto.RequestGroRole && !dto.DepartmentId.HasValue)
        {
            throw new ValidationException("Department is required when requesting GRO role.");
        }
        if (dto.DepartmentId.HasValue)
        {
             await _departmentRepository.Get(dto.DepartmentId.Value);

        }
        var created = await _employeeRepository.Create(employee);


        // if request for gro create role request
        if (dto.RequestGroRole)
        {

            var roleRequest = new RoleRequest
            {
                EmployeeId = created.EmployeeId,
                CurrentRoleId = EMPLOYEE_ROLE_ID,
                RequestedRoleId = GRO_ROLE_ID,
                RequestStatusId = ROLE_REQUEST_PENDING_STATUS_ID,
                CreatedAt = DateTime.UtcNow
            };

            await _roleRequestRepository.Create(roleRequest);
        }

        // Reload with navigation for role name
        var reloaded = await _employeeRepository.GetByEmailAsync(created.Email);

        var token = _tokenService.GenerateToken(reloaded!);
        return new LoginResponseDto
        {
            Token = token,
            EmployeeName = reloaded!.EmployeeName,
            Role = reloaded.Role?.RoleName ?? string.Empty,
            EmployeeId = reloaded.EmployeeId
        };
    }
}
