using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class Role
{
    public short RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public string? Description { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual ICollection<EscalationRule> EscalationRules { get; set; } = new List<EscalationRule>();

    public virtual ICollection<RoleRequest> RoleRequests { get; set; } = new List<RoleRequest>();
}
