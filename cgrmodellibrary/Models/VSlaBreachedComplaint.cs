using System;
using System.Collections.Generic;

namespace cgrmodellibrary.Models;

public partial class VSlaBreachedComplaint
{
    public int? ComplaintId { get; set; }

    public string? ComplaintTitle { get; set; }

    public short? EscalationLevel { get; set; }

    public DateTime? EscalationDueAt { get; set; }

    public int? CurrentHandlerEmployeeId { get; set; }

    public short? PriorityId { get; set; }

    public int? CategoryId { get; set; }

    public short? StatusId { get; set; }

    public DateTime? CreatedAt { get; set; }
}
