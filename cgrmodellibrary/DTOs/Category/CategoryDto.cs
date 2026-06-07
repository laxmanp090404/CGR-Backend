namespace cgrmodellibrary.DTOs.Category;

public class CategoryDto
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public int DepartmentId { get; set; }

    public string DepartmentName { get; set; } = string.Empty;

    public short DefaultPriorityId { get; set; }

    public string DefaultPriorityName { get; set; } = string.Empty;

    public int SlaHours { get; set; }

    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    public List<EscalationRuleEntryDto> EscalationRules { get; set; }
        = new();
}