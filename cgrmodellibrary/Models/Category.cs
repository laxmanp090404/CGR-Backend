using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public int DepartmentId { get; set; }

    public short DefaultPriorityId { get; set; }

    public int SlaHours { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();

    public virtual Priority DefaultPriority { get; set; } = null!;

    public virtual Department Department { get; set; } = null!;

    public virtual ICollection<EscalationRule> EscalationRules { get; set; } = new List<EscalationRule>();
}
