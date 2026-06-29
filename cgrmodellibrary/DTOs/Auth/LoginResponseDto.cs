namespace cgrmodellibrary.DTOs.Auth;

public class LoginResponseDto
{
    public string Token { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
    public string EmployeeName { get; set; } = null!;
    public string Role { get; set; } = null!;
    public int EmployeeId { get; set; }
}
