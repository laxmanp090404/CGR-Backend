namespace cgrmodellibrary.DTOs.Complaint;

public class ComplaintHistoryDto
{
    public int HistoryId { get; set; }
    public int ComplaintId { get; set; }
    public short? OldStatusId { get; set; }
    public string? OldStatusName { get; set; }
    public short? NewStatusId { get; set; }
    public string? NewStatusName { get; set; }
    public int? OldHandlerEmployeeId { get; set; }
    public string? OldHandlerName { get; set; }
    public int? NewHandlerEmployeeId { get; set; }
    public string? NewHandlerName { get; set; }
    public string? Remarks { get; set; }
    public int? ChangedBy { get; set; }
    public string? ChangedByName { get; set; }
    public DateTime CreatedAt { get; set; }
}
