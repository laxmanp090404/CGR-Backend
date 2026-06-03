using System.ComponentModel.DataAnnotations;

namespace cgrmodellibrary.DTOs.Category;

public class CreateCategoryDto
{
    [Required]
    [MaxLength(100)]
    public string CategoryName { get; set; } = null!;
    [Required]
    public int DepartmentId { get; set; }
    [Required]
    public short DefaultPriorityId { get; set; }
    [Required]
    [Range(1, 8760)]
    public int SlaHours { get; set; }
}
