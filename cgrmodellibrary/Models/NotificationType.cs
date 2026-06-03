using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class NotificationType
{
    public short NotificationTypeId { get; set; }

    public string NotificationTypeName { get; set; } = null!;

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
