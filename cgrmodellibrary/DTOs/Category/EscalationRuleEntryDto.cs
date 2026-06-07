namespace cgrmodellibrary.DTOs.Category;

public class EscalationRuleEntryDto
{
    public short PriorityId { get; set; }

    public short EscalationLevel { get; set; }

    public int EscalateAfterHours { get; set; }
}