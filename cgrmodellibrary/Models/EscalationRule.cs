using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class EscalationRule
{
    public int RuleId { get; set; }

    public string RuleName { get; set; } = null!;

    public short? PriorityId { get; set; }

    public int? CategoryId { get; set; }

    public short EscalationLevel { get; set; }

    public short TriggerAfterHours { get; set; }

    public short EscalateToRoleId { get; set; }

    public bool NotifyByEmail { get; set; }

    public bool NotifyByInapp { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Category? Category { get; set; }

    public virtual Role EscalateToRole { get; set; } = null!;

    public virtual ICollection<Escalation> Escalations { get; set; } = new List<Escalation>();

    public virtual Priority? Priority { get; set; }
}
