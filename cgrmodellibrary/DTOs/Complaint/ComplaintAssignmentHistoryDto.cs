namespace cgrmodellibrary.DTOs.Complaint;
public class ComplaintAssignmentHistoryDto
{
    public int AssignmentHistoryId { get; set; }

    public int ComplaintId { get; set; }

    public int? OldHandlerEmployeeId { get; set; }

    public string? OldHandlerEmployeeName { get; set; }

    public int NewHandlerEmployeeId { get; set; }

    public string NewHandlerEmployeeName { get; set; }
        = string.Empty;

    public int? AssignedBy { get; set; }

    public string? AssignedByName { get; set; }

    public string? AssignmentReason { get; set; }

    public DateTime AssignedAt { get; set; }
}