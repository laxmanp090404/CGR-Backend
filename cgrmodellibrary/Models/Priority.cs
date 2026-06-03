using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class Priority
{
    public short PriorityId { get; set; }

    public string PriorityName { get; set; } = null!;

    public short EscalationOrder { get; set; }

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public virtual ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();

    public virtual ICollection<EscalationRule> EscalationRules { get; set; } = new List<EscalationRule>();
}
