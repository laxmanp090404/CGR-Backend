using cgrbussinesslogic.Interfaces;
using cgrmodellibrary.Extensions;
using Microsoft.AspNetCore.Http;

namespace cgrbussinesslogic.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

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
}
