namespace cgrmodellibrary.DTOs.ComplaintRequest;

public class ComplaintRequestDto
{
    public int RequestId { get; set; }
    public int ComplaintId { get; set; }
    public string? ComplaintTitle { get; set; }
    public short RequestTypeId { get; set; }
    public string RequestTypeName { get; set; } = null!;
    public int RequestedBy { get; set; }
    public string RequestedByName { get; set; } = null!;
    public int? ReviewedBy { get; set; }
    public string? ReviewedByName { get; set; }
    public short RequestStatusId { get; set; }
    public string RequestStatusName { get; set; } = null!;
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
