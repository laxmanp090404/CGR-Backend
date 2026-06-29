namespace cgrmodellibrary.DTOs.RoleRequest;

public class RoleRequestDto
{
    public int RoleRequestId { get; set; }
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = null!;
    public short CurrentRoleId { get; set; }
    public string CurrentRoleName { get; set; } = null!;
    public short RequestedRoleId { get; set; }
    public string RequestedRoleName { get; set; } = null!;
    public short RequestStatusId { get; set; }
    public string RequestStatusName { get; set; } = null!;
    public int? ReviewedBy { get; set; }
    public string? ReviewedByName { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? CurrentDepartmentName { get; set; }
}
