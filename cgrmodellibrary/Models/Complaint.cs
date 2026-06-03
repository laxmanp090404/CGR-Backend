using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class Complaint
{
    public int ComplaintId { get; set; }

    public string ComplaintTitle { get; set; } = null!;

    public string ComplaintDescription { get; set; } = null!;

    public int RaisedByEmployeeId { get; set; }

    public int? CurrentHandlerEmployeeId { get; set; }

    public int CategoryId { get; set; }

    public short PriorityId { get; set; }

    public short StatusId { get; set; }

    public short EscalationLevel { get; set; }

    public short ReopenedCount { get; set; }

    public DateTime? EscalationDueAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<ComplaintAssignmentHistory> ComplaintAssignmentHistories { get; set; } = new List<ComplaintAssignmentHistory>();

    public virtual ICollection<ComplaintAttachment> ComplaintAttachments { get; set; } = new List<ComplaintAttachment>();

    public virtual ICollection<ComplaintComment> ComplaintComments { get; set; } = new List<ComplaintComment>();

    public virtual ICollection<ComplaintEscalation> ComplaintEscalations { get; set; } = new List<ComplaintEscalation>();

    public virtual ICollection<ComplaintHistory> ComplaintHistories { get; set; } = new List<ComplaintHistory>();

    public virtual ICollection<ComplaintRequest> ComplaintRequests { get; set; } = new List<ComplaintRequest>();

    public virtual Employee? CurrentHandlerEmployee { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual Priority Priority { get; set; } = null!;

    public virtual Employee RaisedByEmployee { get; set; } = null!;

    public virtual ComplaintStatus Status { get; set; } = null!;
}
