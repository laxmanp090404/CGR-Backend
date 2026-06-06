using System.ComponentModel.DataAnnotations;

namespace cgrmodellibrary.DTOs.Complaint;

public class ResolveComplaintDto
{
    [Required]
    public string ResolutionRemarks { get; set; } = null!;
}

public class RejectComplaintDto
{
    [Required]
    public string RejectionRemarks { get; set; } = null!;
}

public class ReopenComplaintDto
{
    [Required]
    public string ReopenRemarks { get; set; } = null!;
}

public class EscalateComplaintDto
{
    public string? Remarks { get; set; }
}

public class AssignComplaintDto
{
    [Required]
    public int GroEmployeeId { get; set; }

    [Required]
    public string Remarks { get; set; } = null!;
}

public class CloseComplaintDto
{
    [Required]
    public string Remarks { get; set; } = null!;
}