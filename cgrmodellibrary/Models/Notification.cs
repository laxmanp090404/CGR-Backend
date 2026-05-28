using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;
public partial class Notification
{
    public long NotificationId { get; set; }

    public int RecipientEmployeeId { get; set; }

    public long? ComplaintId { get; set; }

    public string NotificationType { get; set; } = null!;

    public string Subject { get; set; } = null!;

    public string Body { get; set; } = null!;

    public string Channel { get; set; } = null!;

    public bool IsSent { get; set; }

    public DateTime? SentAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual Complaint? Complaint { get; set; }

    public virtual Employee RecipientEmployee { get; set; } = null!;
}
