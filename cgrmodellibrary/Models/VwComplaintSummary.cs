using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class VwComplaintSummary
{
    public long? ComplaintId { get; set; }

    public string? ComplaintNumber { get; set; }

    public string? Title { get; set; }

    public string? CategoryName { get; set; }

    public string? PriorityName { get; set; }

    public decimal? SlaMultiplier { get; set; }

    public string? StatusName { get; set; }

    public bool? IsTerminal { get; set; }

    public string? DepartmentName { get; set; }

    public string? RaisedByName { get; set; }

    public string? RaisedByEmail { get; set; }

    public string? AssignedToName { get; set; }

    public DateTime? AssignedAt { get; set; }

    public DateTime? SlaDeadline { get; set; }

    public bool? SlaBreached { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public decimal? SlaHoursRemaining { get; set; }
}
