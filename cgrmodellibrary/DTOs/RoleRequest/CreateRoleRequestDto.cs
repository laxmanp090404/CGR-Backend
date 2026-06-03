using System.ComponentModel.DataAnnotations;

namespace cgrmodellibrary.DTOs.RoleRequest;

public class CreateRoleRequestDto
{
    [Required]
    public short RequestedRoleId { get; set; }
}

public class ReviewRoleRequestDto
{
    [Required]
    public bool Approve { get; set; }
    public string? Remarks { get; set; }
    public int? AssignDepartmentId { get; set; }
}
