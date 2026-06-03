using cgrmodellibrary.DTOs.Auth;

namespace cgrbussinesslogic.Interfaces;

public interface IAuthService
{
    Task<LoginResponseDto> LoginAsync(LoginRequestDto dto);
    Task<LoginResponseDto> RegisterAsync(RegisterRequestDto dto);
}
