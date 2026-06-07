using System.ComponentModel.DataAnnotations;

namespace cgrmodellibrary.DTOs.Category;

public class CreateCategoryDto
{
    [Required]
    [MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Required]
    public int DepartmentId { get; set; }

    [Required]
    public short DefaultPriorityId { get; set; }
    //max 15 days
    [Range(1, 360)]
    public int SlaHours { get; set; }

    public List<EscalationRuleEntryDto> EscalationRules { get; set; }
        = new();
}