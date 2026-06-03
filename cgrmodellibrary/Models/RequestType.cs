using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class RequestType
{
    public short RequestTypeId { get; set; }

    public string RequestTypeName { get; set; } = null!;

    public virtual ICollection<ComplaintRequest> ComplaintRequests { get; set; } = new List<ComplaintRequest>();
}
