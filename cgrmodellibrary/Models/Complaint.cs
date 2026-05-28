using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;
public partial class Complaint
{
    public long ComplaintId { get; set; }

    public string ComplaintNumber { get; set; } = null!;

    public string Title { get; set; } = null!;

    public string Description { get; set; } = null!;

    public string? ImpactDescription { get; set; }

    public int CategoryId { get; set; }

    public short? PriorityId { get; set; }

    public short StatusId { get; set; }

    public int RaisedByEmployeeId { get; set; }

    public int? AssignedToEmployeeId { get; set; }

    public int DepartmentId { get; set; }

    public DateTime? SlaDeadline { get; set; }

    public bool SlaBreached { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public string? ResolutionRemarks { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Employee? AssignedToEmployee { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<ComplaintAttachment> ComplaintAttachments { get; set; } = new List<ComplaintAttachment>();

    public virtual ICollection<ComplaintComment> ComplaintComments { get; set; } = new List<ComplaintComment>();

    public virtual ComplaintOfficer? ComplaintOfficer { get; set; }

    public virtual ICollection<ComplaintStatusHistory> ComplaintStatusHistories { get; set; } = new List<ComplaintStatusHistory>();

    public virtual Department Department { get; set; } = null!;

    public virtual ICollection<Escalation> Escalations { get; set; } = new List<Escalation>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Priority? Priority { get; set; }

    public virtual Employee RaisedByEmployee { get; set; } = null!;

    public virtual ComplaintStatus Status { get; set; } = null!;
}
