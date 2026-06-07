namespace cgrmodellibrary.DTOs.Employee;
public class EmployeeDto
{
    public int EmployeeId { get; set; }

    public string EmployeeName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string MobileNumber { get; set; } = string.Empty;

    public short RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;

    public int? DepartmentId { get; set; }

    public string? DepartmentName { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}