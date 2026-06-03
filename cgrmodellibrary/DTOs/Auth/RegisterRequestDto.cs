using System.ComponentModel.DataAnnotations;

namespace cgrmodellibrary.DTOs.Auth;

public class RegisterRequestDto
{
    public string EmployeeName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string MobileNumber { get; set; } = null!;
    public string Password { get; set; } = null!;
    public int? DepartmentId { get; set; }
    public bool RequestGroRole { get; set; }
}
