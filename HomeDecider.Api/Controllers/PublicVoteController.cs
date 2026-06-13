using HomeDecider.Api.Dtos;
using HomeDecider.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeDecider.Api.Controllers;

[ApiController]
[Route("api/public")]
[AllowAnonymous]
public class PublicVoteController(IDecisionService decisionService) : ControllerBase
{
    [HttpGet("{token}")]
    public async Task<IActionResult> GetInfo(string token)
    {
        var info = await decisionService.GetPublicInfoAsync(token);
        if (info is null) return NotFound();
        return Ok(info);
    }

    [HttpPost("{token}/poll")]
    public async Task<IActionResult> Poll(string token, [FromBody] PublicPollRequest request)
    {
        var (decision, error) = await decisionService.GetPublicStateAsync(token, request.VoterName, request.Password);
        return error switch
        {
            "wrong_password" => Unauthorized("Senha incorreta."),
            "not_found"      => NotFound(),
            _                => Ok(decision)
        };
    }

    [HttpPost("{token}/vote")]
    public async Task<IActionResult> Vote(string token, [FromBody] PublicVoteRequest request)
    {
        var (decision, error) = await decisionService.CastPublicVoteAsync(token, request.VoterName, request.OptionId, request.Password);
        return error switch
        {
            "wrong_password" => Unauthorized("Senha incorreta."),
            "not_found"      => NotFound(),
            "resolved"       => BadRequest("Decisão já encerrada."),
            "invalid_option" => BadRequest("Opção inválida."),
            _                => Ok(decision)
        };
    }
}
