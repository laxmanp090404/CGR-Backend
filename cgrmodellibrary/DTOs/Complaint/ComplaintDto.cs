namespace cgrmodellibrary.DTOs.Complaint;

public class ComplaintDto
{
    public int ComplaintId { get; set; }
    public string ComplaintTitle { get; set; } = null!;
    public string ComplaintDescription { get; set; } = null!;
    public int RaisedByEmployeeId { get; set; }
    public string RaisedByEmployeeName { get; set; } = null!;
    public int? CurrentHandlerEmployeeId { get; set; }
    public string? CurrentHandlerEmployeeName { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = null!;
    public short PriorityId { get; set; }
    public string PriorityName { get; set; } = null!;
    public short StatusId { get; set; }
    public string StatusName { get; set; } = null!;
    public short EscalationLevel { get; set; }
    public short ReopenedCount { get; set; }
    public DateTime? EscalationDueAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}
