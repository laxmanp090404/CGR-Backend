namespace cgrmodellibrary.DTOs.Complaint;

public class ComplaintDashboardDto
{
    public int ComplaintId { get; set; }
    public string? ComplaintTitle { get; set; }
    public string? StatusName { get; set; }
    public string? PriorityName { get; set; }
    public string? CategoryName { get; set; }
    public string? DepartmentName { get; set; }
    public string? RaisedByName { get; set; }
    public string? CurrentHandlerName { get; set; }
    public short EscalationLevel { get; set; }
    public DateTime? EscalationDueAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}
