using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;
public partial class Department
{
    public int DepartmentId { get; set; }

    public string DepartmentName { get; set; } = null!;

    public int? ParentDeptId { get; set; }

    public int? HeadEmployeeId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();

    public virtual Employee? HeadEmployee { get; set; }

    public virtual ICollection<Department> InverseParentDept { get; set; } = new List<Department>();

    public virtual Department? ParentDept { get; set; }

    public virtual ICollection<RoleRequest> RoleRequests { get; set; } = new List<RoleRequest>();
}
