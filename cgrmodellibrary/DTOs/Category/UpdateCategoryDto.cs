using System.ComponentModel.DataAnnotations;

namespace cgrmodellibrary.DTOs.Category;

public class UpdateCategoryDto
{
    [Required]
    [MaxLength(100)]
    public string CategoryName { get; set; } = null!;
    [Required]
    public short DefaultPriorityId { get; set; }
    [Required]
    [Range(1, 8760)]
    public int SlaHours { get; set; }
    public bool IsActive { get; set; }
}
