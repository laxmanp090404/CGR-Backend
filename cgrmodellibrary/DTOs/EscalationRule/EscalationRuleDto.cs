namespace cgrmodellibrary.DTOs.EscalationRule;

public class EscalationRuleDto
{
    public int EscalationRuleId { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public short PriorityId { get; set; }
    public string PriorityName { get; set; } = null!;
    public short EscalationLevel { get; set; }
    public int EscalateAfterHours { get; set; }
   
}
