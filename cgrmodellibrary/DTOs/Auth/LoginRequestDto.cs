using System.ComponentModel.DataAnnotations;

namespace cgrmodellibrary.DTOs.Auth;

public class LoginRequestDto
{
    [Required(ErrorMessage = "Email is required.")]
    [RegularExpression(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", ErrorMessage = "Invalid email format.")]
    public string Email { get; set; } = null!;
    [Required(ErrorMessage = "Password is required.")]
    public string Password { get; set; } = null!;
}
