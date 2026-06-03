using System.ComponentModel.DataAnnotations;

namespace cgrmodellibrary.DTOs.RoleRequest;

public class ManualRoleChangeDto
{
    [Required]
    public short TargetRoleId { get; set; }
    public int? DepartmentId { get; set; }

    [StringLength(100)]
    public string? Remarks { get; set; }
}