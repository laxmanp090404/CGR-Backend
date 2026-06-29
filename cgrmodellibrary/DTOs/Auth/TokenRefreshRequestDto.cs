namespace cgrmodellibrary.DTOs.Auth;

public class TokenRefreshRequestDto
{
    public string AccessToken { get; set; } = null!;
    public string RefreshToken { get; set; } = null!;
}
