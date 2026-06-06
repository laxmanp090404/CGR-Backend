using cgrbussinesslogic.Interfaces;
using cgrmodellibrary.Exceptions;
using cgrmodellibrary.Extensions;
using Microsoft.AspNetCore.Http;

namespace cgrbussinesslogic.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const short ROLE_EMPLOYEE = 1;
    private const short ROLE_GRO = 2;
    private const short ROLE_DEPARTMENT_HEAD = 3;
    private const short ROLE_ADMIN = 4;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private System.Security.Claims.ClaimsPrincipal? User =>
        _httpContextAccessor.HttpContext?.User;

    public int EmployeeId =>
        User?.GetEmployeeId() ?? throw new UnauthorizedAccessException("Not authenticated.");

    public string Role =>
        User?.GetRole() ?? throw new UnauthorizedAccessException("Not authenticated.");

    public int? DepartmentId =>
        User?.GetDepartmentId();

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated == true;

    public short RoleId => GetCurrentRoleId(Role);

    private short GetCurrentRoleId(string role)
    {
        switch (role.ToUpper())
        {
            case "EMPLOYEE":
                return ROLE_EMPLOYEE;

            case "GRO":
                return ROLE_GRO;

            case "DEPARTMENT_HEAD":
                return ROLE_DEPARTMENT_HEAD;

            case "ADMIN":
                return ROLE_ADMIN;

            default:
                throw new BusinessRuleException("Unknown role.");
        }
    }
}
