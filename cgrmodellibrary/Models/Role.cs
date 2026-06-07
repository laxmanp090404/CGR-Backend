using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class Role
{
    public short RoleId { get; set; }

    public string RoleName { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    // it should be a collection as we nede many ie more employee can have same role
    // admin only one person ie enforded via the partial index
    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
   

    public virtual ICollection<RoleRequest> RoleRequestCurrentRoles { get; set; } = new List<RoleRequest>();

    public virtual ICollection<RoleRequest> RoleRequestRequestedRoles { get; set; } = new List<RoleRequest>();
}
