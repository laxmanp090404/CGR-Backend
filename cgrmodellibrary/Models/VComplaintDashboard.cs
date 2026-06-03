using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class VComplaintDashboard
{
    public int? ComplaintId { get; set; }

    public string? ComplaintTitle { get; set; }

    public short? EscalationLevel { get; set; }

    public short? ReopenedCount { get; set; }

    public DateTime? EscalationDueAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public string? StatusName { get; set; }

    public bool? IsTerminal { get; set; }

    public string? PriorityName { get; set; }

    public short? PriorityWeight { get; set; }

    public string? CategoryName { get; set; }

    public string? DepartmentName { get; set; }

    public string? RaisedByName { get; set; }

    public string? CurrentHandlerName { get; set; }

    public string? CurrentHandlerRole { get; set; }
}
