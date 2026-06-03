using System.ComponentModel.DataAnnotations;

namespace cgrmodellibrary.DTOs.ComplaintRequest;

public class CreateComplaintRequestDto
{
    [Required]
    public short RequestTypeId { get; set; }
    public string? Remarks { get; set; }
}

public class ReviewComplaintRequestDto
{
    [Required]
    public bool Approve { get; set; }
    public string? Remarks { get; set; }
}
