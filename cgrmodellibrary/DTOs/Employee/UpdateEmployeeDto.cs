namespace cgrmodellibrary.DTOs.Employee;

public class UpdateEmployeeDto
{
    public string EmployeeName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string MobileNumber { get; set; } = null!;
    public int RoleId { get; set; }
    public int? DepartmentId { get; set; }
    public bool IsActive { get; set; }
}
