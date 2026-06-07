namespace cgrmodellibrary.DTOs.Department;

public class DepartmentDto
{
    public int DepartmentId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public int? DepartmentHeadEmployeeId { get; set; }

    public string? DepartmentHeadEmployeeName { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
}