using cgrbussinesslogic.Interfaces;
using cgrmodellibrary.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;

namespace cgrwebapi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    //POST api/auth/login
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return Ok(result);
    }

    // POST api/auth/register
    [HttpPost("register")]
    public async Task<ActionResult<LoginResponseDto>> Register([FromBody] RegisterRequestDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        return StatusCode(StatusCodes.Status201Created, result);
    }
}
