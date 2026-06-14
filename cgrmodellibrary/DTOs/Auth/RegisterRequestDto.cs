using System.ComponentModel.DataAnnotations;

namespace cgrmodellibrary.DTOs.Auth;

public class RegisterRequestDto
{
    [Required(ErrorMessage = "Employee name is required.")]
    public string EmployeeName { get; set; } = null!;
    [Required(ErrorMessage = "Email is required.")]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = null!;
    [Required(ErrorMessage = "Mobile number is required.")]
    [StringLength(10, ErrorMessage = "Mobile number must be 10 digits.")]
    public string MobileNumber { get; set; } = null!;
    [Required(ErrorMessage = "Password is required.")] 
    public string Password { get; set; } = null!;
    public int? DepartmentId { get; set; }
    [Required(ErrorMessage = "Requesting GRO role is required.")]
    public bool RequestGroRole { get; set; }
}
