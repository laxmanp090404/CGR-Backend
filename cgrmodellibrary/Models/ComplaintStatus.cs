using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;
public partial class ComplaintStatus
{
    public short StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public bool IsTerminal { get; set; }

    public short DisplayOrder { get; set; }

    public virtual ICollection<ComplaintStatusHistory> ComplaintStatusHistoryFromStatuses { get; set; } = new List<ComplaintStatusHistory>();

    public virtual ICollection<ComplaintStatusHistory> ComplaintStatusHistoryToStatuses { get; set; } = new List<ComplaintStatusHistory>();

    public virtual ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
}
