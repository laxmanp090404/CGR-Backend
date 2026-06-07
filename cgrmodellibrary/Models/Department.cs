using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class Department
{
    public int DepartmentId { get; set; }

    public string DepartmentName { get; set; } = null!;

    public int? DepartmentHeadEmployeeId { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public virtual Employee? DepartmentHeadEmployee { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
