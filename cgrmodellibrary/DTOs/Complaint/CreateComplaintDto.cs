using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace cgrmodellibrary.DTOs.Complaint;

public class CreateComplaintDto
{
    [Required]
    [MaxLength(250)]
    public string ComplaintTitle { get; set; } = null!;

    [Required]
    public string ComplaintDescription { get; set; } = null!;

    [Required]
    public int CategoryId { get; set; }

    public List<IFormFile>? Attachments { get; set; }
}