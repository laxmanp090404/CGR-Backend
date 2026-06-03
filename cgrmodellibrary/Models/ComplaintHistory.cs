using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class ComplaintHistory
{
    public int HistoryId { get; set; }

    public int ComplaintId { get; set; }

    public short? OldStatusId { get; set; }

    public short NewStatusId { get; set; }

    public short EscalationLevelSnapshot { get; set; }

    public int? OldHandlerEmployeeId { get; set; }

    public int? NewHandlerEmployeeId { get; set; }

    public int? ChangedBy { get; set; }

    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Employee? ChangedByNavigation { get; set; }

    public virtual Complaint Complaint { get; set; } = null!;

    public virtual Employee? NewHandlerEmployee { get; set; }

    public virtual ComplaintStatus NewStatus { get; set; } = null!;

    public virtual Employee? OldHandlerEmployee { get; set; }

    public virtual ComplaintStatus? OldStatus { get; set; }
}
