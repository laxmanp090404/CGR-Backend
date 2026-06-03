using System.ComponentModel.DataAnnotations;

namespace cgrmodellibrary.DTOs.EscalationRule;

public class CreateEscalationRuleDto
{
    [Required]
    public int CategoryId { get; set; }
    [Required]
    public short PriorityId { get; set; }
    [Required]
    [Range(1, 10)]
    public short EscalationLevel { get; set; }
    [Required]
    [Range(1, 8760)]
    public int EscalateAfterHours { get; set; }
    [Required]
    public short EscalateToRoleId { get; set; }
}
