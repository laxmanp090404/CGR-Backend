using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class ComplaintEscalation
{
    public int EscalationId { get; set; }

    public int ComplaintId { get; set; }

    public int EscalatedToEmployeeId { get; set; }

    public short EscalationLevel { get; set; }

    public int? EscalationRuleId { get; set; }

    public int? EscalatedByEmployeeId { get; set; }

    public string? Reason { get; set; }

    public DateTime EscalatedAt { get; set; }

    public virtual Complaint Complaint { get; set; } = null!;

    public virtual Employee? EscalatedByEmployee { get; set; }

    public virtual Employee EscalatedToEmployee { get; set; } = null!;

    public virtual EscalationRule? EscalationRule { get; set; }
}
