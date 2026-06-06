using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace cgrmodellibrary.DTOs.Complaint;

public class CreateComplaintDto
{
    [Required(ErrorMessage = "Complaint title is required.")]
    [MaxLength(250, ErrorMessage = "Complaint title cannot exceed 250 characters.")]
    public string ComplaintTitle { get; set; } = null!;

    [Required(ErrorMessage = "Complaint description is required.")]
    public string ComplaintDescription { get; set; } = null!;

    [Required(ErrorMessage = "Category is required.")]
    public int CategoryId { get; set; }

    public List<IFormFile>? Attachments { get; set; }
}