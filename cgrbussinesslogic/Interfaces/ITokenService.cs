using cgrmodellibrary.Models;

namespace cgrbussinesslogic.Interfaces;

public interface ITokenService
{
    string GenerateToken(Employee employee);
}
