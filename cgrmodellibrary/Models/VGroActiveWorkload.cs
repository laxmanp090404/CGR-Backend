using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class VGroActiveWorkload
{
    public int? EmployeeId { get; set; }

    public string? EmployeeName { get; set; }

    public int? DepartmentId { get; set; }

    public DateTime? EmployeeCreatedAt { get; set; }

    public long? ActiveComplaintCount { get; set; }

    public long? WeightedScore { get; set; }
}
