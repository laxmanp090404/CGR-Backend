namespace cgrmodellibrary.DTOs.Department;

public class DepartmentDto
{
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = null!;
    public int? HeadEmployeeId { get; set; }
    public string? HeadEmployeeName { get; set; }
    public DateTime CreatedAt { get; set; }
}
