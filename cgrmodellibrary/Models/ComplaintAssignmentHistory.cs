using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class ComplaintAssignmentHistory
{
    public int AssignmentHistoryId { get; set; }

    public int ComplaintId { get; set; }

    public int? OldHandlerEmployeeId { get; set; }

    public int NewHandlerEmployeeId { get; set; }

    public int AssignedBy { get; set; }

    public string AssignmentReason { get; set; } = null!;

    public DateTime AssignedAt { get; set; }

    public virtual Employee AssignedByNavigation { get; set; } = null!;

    public virtual Complaint Complaint { get; set; } = null!;

    public virtual Employee NewHandlerEmployee { get; set; } = null!;

    public virtual Employee? OldHandlerEmployee { get; set; }
}
