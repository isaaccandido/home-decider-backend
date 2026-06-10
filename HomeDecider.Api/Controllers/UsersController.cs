using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HomeDecider.Api.Dtos;
using HomeDecider.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeDecider.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController(IUserService userService) : ControllerBase
{
    private string Actor => User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)!;
    private bool IsAdmin => User.FindFirstValue("isAdmin") == "true";

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!IsAdmin) return Forbid();
        return Ok(await userService.GetAllAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        if (!IsAdmin) return Forbid();
        if (string.IsNullOrWhiteSpace(request.Username)) return BadRequest("Username is required.");
        if (string.IsNullOrWhiteSpace(request.Password)) return BadRequest("Password is required.");

        var (user, error) = await userService.CreateAsync(request, Actor);
        if (error is not null) return Conflict(error);
        return CreatedAtAction(nameof(GetAll), new { id = user!.Id }, user);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        if (!IsAdmin) return Forbid();
        var user = await userService.UpdateAsync(id, request, Actor);
        if (user is null) return NotFound();
        return Ok(user);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAdmin) return Forbid();
        var currentUserId = int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
        if (id == currentUserId) return BadRequest("Cannot delete yourself.");

        var deleted = await userService.DeleteAsync(id, currentUserId, Actor);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
