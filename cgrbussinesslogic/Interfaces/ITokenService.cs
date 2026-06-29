using cgrmodellibrary.Models;

namespace cgrbussinesslogic.Interfaces;

public interface ITokenService
{
    string GenerateToken(Employee employee);
    string GenerateRefreshToken();
    System.Security.Claims.ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
