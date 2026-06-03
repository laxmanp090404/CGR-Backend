using System.ComponentModel.DataAnnotations;

namespace cgrmodellibrary.DTOs.Department;

public class UpdateDepartmentDto
{
    [Required]
    [MaxLength(100)]
    public string DepartmentName { get; set; } = null!;
    public int? HeadEmployeeId { get; set; }
}
