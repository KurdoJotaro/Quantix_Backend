using Microsoft.AspNetCore.Mvc;
using Quantix.Core.DTOs;
using Quantix.Core.Interfaces;

namespace Quantix.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var result = await _authService.RegisterAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _authService.LoginAsync(dto);
        return result.Success ? Ok(result) : Unauthorized(result);
    }

    [HttpGet("verify-email")]
    public async Task<IActionResult> VerifyEmail([FromQuery] string userId, [FromQuery] string token)
    {
        var dto = new VerifyEmailDto { UserId = userId, Token = token };
        var result = await _authService.VerifyEmailAsync(dto);
        return result.Success ? Ok(result) : BadRequest(result);
    }
}
