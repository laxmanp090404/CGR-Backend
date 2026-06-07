using System.ComponentModel.DataAnnotations;

namespace cgrmodellibrary.DTOs.Department;

public class UpdateDepartmentDto
{
    [Required(ErrorMessage = "Department name is required.")]
    [MaxLength(100)]
    public string DepartmentName { get; set; } = string.Empty;

    public int? DepartmentHeadEmployeeId { get; set; }

    public bool IsActive { get; set; }
}