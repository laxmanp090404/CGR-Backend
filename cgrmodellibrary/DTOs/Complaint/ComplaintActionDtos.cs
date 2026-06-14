using System.ComponentModel.DataAnnotations;

namespace cgrmodellibrary.DTOs.Complaint;

public class ResolveComplaintDto
{
    [Required(ErrorMessage = "Resolution remarks are required.")]
    [MaxLength(100, ErrorMessage = "Resolution remarks cannot exceed 100 characters.")]
    public string ResolutionRemarks { get; set; } = null!;
}

public class RejectComplaintDto
{
    [Required(ErrorMessage = "Rejection remarks are required.")]
    [MaxLength(100, ErrorMessage = "Rejection remarks cannot exceed 100 characters.")]     
    public string RejectionRemarks { get; set; } = null!;
}

public class ReopenComplaintDto
{
    [Required(ErrorMessage = "Reopen remarks are required.")]
    [MaxLength(100, ErrorMessage = "Reopen remarks cannot exceed 100 characters.")]
    public string ReopenRemarks { get; set; } = null!;
}

public class EscalateComplaintDto
{
    [Required(ErrorMessage = "Escalation remarks are required.")]
    [MaxLength(100, ErrorMessage = "Escalation remarks cannot exceed 100 characters.")]
    public string? Remarks { get; set; }
}

public class AssignComplaintDto
{
    [Required(ErrorMessage = "Gro employee ID is required.")]
    public int GroEmployeeId { get; set; }

    [Required(ErrorMessage = "Remarks are required.")]
    [MaxLength(100, ErrorMessage = "Remarks cannot exceed 100 characters.")]
    public string Remarks { get; set; } = null!;
}

public class CloseComplaintDto
{
    [Required(ErrorMessage = "Remarks are required.")]
    [MaxLength(100, ErrorMessage = "Remarks cannot exceed 100 characters.")]
    public string Remarks { get; set; } = null!;
}