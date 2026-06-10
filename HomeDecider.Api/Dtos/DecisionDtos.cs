namespace HomeDecider.Api.Dtos;

public record CreateDecisionRequest(
    string Title,
    string? Considerations,
    List<string> Options,
    bool AllowMultipleVotes,
    bool IsAnonymous
);

public record CastVoteRequest(int OptionId);

public record OptionDto(int Id, string Text, List<string> Voters);

public record VoteDto(string VoterName, int OptionId, string OptionText);

public record DecisionDto(
    int Id,
    string Title,
    string? Considerations,
    DateTime CreatedAt,
    bool IsResolved,
    int? WinnerOptionId,
    bool AllowMultipleVotes,
    bool IsAnonymous,
    List<OptionDto> Options,
    List<VoteDto> Votes
);

public record DecisionSummaryDto(
    int Id,
    string Title,
    DateTime CreatedAt,
    bool IsResolved,
    string? WinnerOptionText
);
