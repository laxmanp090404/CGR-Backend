using System.ComponentModel.DataAnnotations;

namespace cgrmodellibrary.DTOs.Category;

public class UpdateCategoryDto
{
    [Required(ErrorMessage = "Category name is required for Update.")]
    [MaxLength(100)]
    public string CategoryName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required.")]
    public int DepartmentId { get; set; }

    [Required(ErrorMessage = "Default priority is required.")]
    public short DefaultPriorityId { get; set; }

    [Range(1, 360, ErrorMessage = "SLA hours must be between 1 and 360.")]
    public int SlaHours { get; set; }

    public bool IsActive { get; set; }

    public List<EscalationRuleEntryDto> EscalationRules { get; set; }
        = new();
}