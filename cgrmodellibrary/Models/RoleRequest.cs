using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;
public partial class RoleRequest
{
    public int RequestId { get; set; }

    public string RequesterName { get; set; } = null!;

    public string RequesterEmail { get; set; } = null!;

    public string? RequesterMobile { get; set; }

    public string? RequesterAddressEncrypted { get; set; }

    public short RequestedRoleId { get; set; }

    public int RequestedDeptId { get; set; }

    public string? Justification { get; set; }

    public string Status { get; set; } = null!;

    public int? ReviewedBy { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewRemarks { get; set; }

    public int? ApprovedEmployeeId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual Employee? ApprovedEmployee { get; set; }

    public virtual Department RequestedDept { get; set; } = null!;

    public virtual Role RequestedRole { get; set; } = null!;

    public virtual Employee? ReviewedByNavigation { get; set; }
}
