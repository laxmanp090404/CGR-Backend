using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class EscalationRule
{
    public int EscalationRuleId { get; set; }

    public int CategoryId { get; set; }

    public short PriorityId { get; set; }

    public short EscalationLevel { get; set; }

    public int EscalateAfterHours { get; set; }

   

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<ComplaintEscalation> ComplaintEscalations { get; set; } = new List<ComplaintEscalation>();



    public virtual Priority Priority { get; set; } = null!;
}
