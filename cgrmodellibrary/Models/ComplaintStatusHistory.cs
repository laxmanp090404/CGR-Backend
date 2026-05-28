using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;
public partial class ComplaintStatusHistory
{
    public long HistoryId { get; set; }

    public long ComplaintId { get; set; }

    public short? FromStatusId { get; set; }

    public short ToStatusId { get; set; }

    public int ChangedByEmployeeId { get; set; }

    public string? Remarks { get; set; }

    public DateTime ChangedAt { get; set; }

    public virtual Employee ChangedByEmployee { get; set; } = null!;

    public virtual Complaint Complaint { get; set; } = null!;

    public virtual ComplaintStatus? FromStatus { get; set; }

    public virtual ComplaintStatus ToStatus { get; set; } = null!;
}
