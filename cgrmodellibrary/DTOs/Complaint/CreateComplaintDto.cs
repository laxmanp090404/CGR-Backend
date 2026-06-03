using System.ComponentModel.DataAnnotations;

namespace cgrmodellibrary.DTOs.Complaint;

public class CreateComplaintDto
{
    [Required]
    [MaxLength(200)]
    public string ComplaintTitle { get; set; } = null!;
    [Required]
    public string ComplaintDescription { get; set; } = null!;
    [Required]
    public int CategoryId { get; set; }
}
