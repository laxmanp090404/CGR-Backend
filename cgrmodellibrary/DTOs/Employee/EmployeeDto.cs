using System;

namespace cgrmodellibrary.DTOs.Employee;

public class EmployeeDto
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string MobileNumber { get; set; } = null!;
    public int RoleId { get; set; }
    public string RoleName { get; set; } = null!;
    public int? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
