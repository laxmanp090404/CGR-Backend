using System.ComponentModel.DataAnnotations;

namespace cgrmodellibrary.DTOs.Employee;
public class UpdateEmployeeDto
{
    [Required(ErrorMessage = "Employee name is required.")]
    public string EmployeeName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mobile number is required.")]
    [StringLength(10, MinimumLength = 10, ErrorMessage = "Mobile number must be exactly 10 digits.")]
    [RegularExpression(@"^\d+$", ErrorMessage = "Mobile number must contain only digits.")]
    public string MobileNumber { get; set; } = string.Empty;
}