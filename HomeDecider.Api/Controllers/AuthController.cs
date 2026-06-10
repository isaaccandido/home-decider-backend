using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HomeDecider.Api.Dtos;
using HomeDecider.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeDecider.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await authService.LoginAsync(request);
        if (response is null) return Unauthorized();
        return Ok(response);
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult Me()
    {
        return Ok(new
        {
            username = User.FindFirstValue(JwtRegisteredClaimNames.UniqueName),
            isAdmin = User.FindFirstValue("isAdmin") == "true",
            canCreatePolls = User.FindFirstValue("canCreatePolls") == "true",
        });
    }
}
