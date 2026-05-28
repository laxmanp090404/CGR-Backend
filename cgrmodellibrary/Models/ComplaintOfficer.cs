using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;
public partial class ComplaintOfficer
{
    public long CoId { get; set; }

    public long ComplaintId { get; set; }

    public int EmployeeId { get; set; }

    public string RoleInComplaint { get; set; } = null!;

    public DateTime AssignedAt { get; set; }

    public int AssignedBy { get; set; }

    public bool IsActive { get; set; }

    public virtual Employee AssignedByNavigation { get; set; } = null!;

    public virtual Complaint Complaint { get; set; } = null!;

    public virtual Employee Employee { get; set; } = null!;
}
