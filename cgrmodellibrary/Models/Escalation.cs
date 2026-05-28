using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class Escalation
{
    public long EscalationId { get; set; }

    public long ComplaintId { get; set; }

    public int RuleId { get; set; }

    public short EscalationLevel { get; set; }

    public int? EscalatedToEmployeeId { get; set; }

    public DateTime EscalatedAt { get; set; }

    public DateTime? AcknowledgedAt { get; set; }

    public int? AcknowledgedBy { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public string? Remarks { get; set; }

    public virtual Employee? AcknowledgedByNavigation { get; set; }

    public virtual Complaint Complaint { get; set; } = null!;

    public virtual Employee? EscalatedToEmployee { get; set; }

    public virtual EscalationRule Rule { get; set; } = null!;
}
