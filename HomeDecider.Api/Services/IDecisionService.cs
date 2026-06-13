using HomeDecider.Api.Dtos;

namespace HomeDecider.Api.Services;

public interface IDecisionService
{
    Task<IEnumerable<DecisionSummaryDto>> GetAllAsync();
    Task<DecisionDto?> GetByIdAsync(int id);
    Task<(int? Id, string? Error)> CreateAsync(CreateDecisionRequest request, string actor);
    Task<(bool Success, string? Error)> CastVoteAsync(int decisionId, int optionId, string voterName);
    Task<(bool Success, string? Error)> ResolveAsync(int decisionId, int winnerOptionId, string actor);

    Task<PublicDecisionInfoDto?> GetPublicInfoAsync(string token);
    Task<(PublicDecisionDto? Decision, string? Error)> GetPublicStateAsync(string token, string voterName, string password);
    Task<(PublicDecisionDto? Decision, string? Error)> CastPublicVoteAsync(string token, string voterName, int optionId, string password);
}
