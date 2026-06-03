using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class RoleRequest
{
    public int RoleRequestId { get; set; }

    public int EmployeeId { get; set; }

    public short CurrentRoleId { get; set; }

    public short RequestedRoleId { get; set; }

    public short RequestStatusId { get; set; }

    public int? ReviewedBy { get; set; }

    public string? Remarks { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public virtual Role CurrentRole { get; set; } = null!;

    public virtual Employee Employee { get; set; } = null!;

    public virtual RequestStatus RequestStatus { get; set; } = null!;

    public virtual Role RequestedRole { get; set; } = null!;

    public virtual Employee? ReviewedByNavigation { get; set; }
}
