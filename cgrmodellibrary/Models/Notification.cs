using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int EmployeeId { get; set; }

    public short NotificationTypeId { get; set; }

    public int? ReferenceComplaintId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Employee Employee { get; set; } = null!;

    public virtual NotificationType NotificationType { get; set; } = null!;

    public virtual Complaint? ReferenceComplaint { get; set; }
}
