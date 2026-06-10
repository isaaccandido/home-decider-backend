using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using HomeDecider.Api.Dtos;
using HomeDecider.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeDecider.Api.Controllers;

[ApiController]
[Route("api/decisions")]
[Authorize]
public class DecisionsController(IDecisionService decisionService) : ControllerBase
{
    private string Actor => User.FindFirstValue(JwtRegisteredClaimNames.UniqueName)!;
    private bool IsAdmin => User.FindFirstValue("isAdmin") == "true";
    private bool CanCreatePolls => User.FindFirstValue("canCreatePolls") == "true";

    [HttpGet]
    public async Task<IActionResult> GetAll() =>
        Ok(await decisionService.GetAllAsync());

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var decision = await decisionService.GetByIdAsync(id);
        if (decision is null) return NotFound();
        return Ok(decision);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDecisionRequest request)
    {
        if (!CanCreatePolls && !IsAdmin) return Forbid();

        var (decisionId, error) = await decisionService.CreateAsync(request, Actor);
        if (error is not null) return BadRequest(error);
        return CreatedAtAction(nameof(GetById), new { id = decisionId }, decisionId);
    }

    [HttpPost("{id:int}/votes")]
    public async Task<IActionResult> CastVote(int id, [FromBody] CastVoteRequest request)
    {
        var (success, error) = await decisionService.CastVoteAsync(id, request.OptionId, Actor);
        if (!success) return error == "Decision not found." ? NotFound() : BadRequest(error);
        return Ok();
    }

    [HttpPost("{id:int}/resolve")]
    public async Task<IActionResult> Resolve(int id, [FromQuery] int winnerOptionId)
    {
        var (success, error) = await decisionService.ResolveAsync(id, winnerOptionId, Actor);
        if (!success) return error == "Decision not found." ? NotFound() : BadRequest(error);
        return Ok();
    }
}
