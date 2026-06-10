using HomeDecider.Api.Dtos;

namespace HomeDecider.Api.Services;

public interface IDecisionService
{
    Task<IEnumerable<DecisionSummaryDto>> GetAllAsync();
    Task<DecisionDto?> GetByIdAsync(int id);
    Task<(int? Id, string? Error)> CreateAsync(CreateDecisionRequest request, string actor);
    Task<(bool Success, string? Error)> CastVoteAsync(int decisionId, int optionId, string voterName);
    Task<(bool Success, string? Error)> ResolveAsync(int decisionId, int winnerOptionId, string actor);
}
