using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class ComplaintStatus
{
    public short StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public bool IsTerminal { get; set; }

    public virtual ICollection<ComplaintHistory> ComplaintHistoryNewStatuses { get; set; } = new List<ComplaintHistory>();

    public virtual ICollection<ComplaintHistory> ComplaintHistoryOldStatuses { get; set; } = new List<ComplaintHistory>();

    public virtual ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}
