using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class RequestStatus
{
    public short RequestStatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public virtual ICollection<ComplaintRequest> ComplaintRequests { get; set; } = new List<ComplaintRequest>();

    public virtual ICollection<RoleRequest> RoleRequests { get; set; } = new List<RoleRequest>();
}
