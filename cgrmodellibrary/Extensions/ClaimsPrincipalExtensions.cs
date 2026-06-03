using System;
using System.Security.Claims;

namespace cgrmodellibrary.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static int GetEmployeeId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("employee_id");
        if (claim == null)
        {
            throw new UnauthorizedAccessException("Employee ID claim not found.");
        }
        return int.Parse(claim.Value);
    }

    public static string GetRole(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.Role) ?? user.FindFirst("role");
        if (claim == null)
        {
            throw new UnauthorizedAccessException("Role claim not found.");
        }
        return claim.Value;
    }

    public static int? GetDepartmentId(this ClaimsPrincipal user)
    {
        var claim = user.FindFirst("department_id");
        return int.TryParse(claim?.Value, out var id) ? id : null;
    }
}
