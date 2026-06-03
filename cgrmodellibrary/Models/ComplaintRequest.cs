using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class ComplaintRequest
{
    public int RequestId { get; set; }

    public int ComplaintId { get; set; }

    public short RequestTypeId { get; set; }

    public int RequestedBy { get; set; }

    public int? ReviewedBy { get; set; }

    public short RequestStatusId { get; set; }

    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public virtual Complaint Complaint { get; set; } = null!;

    public virtual RequestStatus RequestStatus { get; set; } = null!;

    public virtual RequestType RequestType { get; set; } = null!;

    public virtual Employee RequestedByNavigation { get; set; } = null!;

    public virtual Employee? ReviewedByNavigation { get; set; }
}
