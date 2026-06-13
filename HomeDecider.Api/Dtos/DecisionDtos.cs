namespace HomeDecider.Api.Dtos;

public record CreateDecisionRequest(
    string Title,
    string? Considerations,
    List<string> Options,
    bool AllowMultipleVotes,
    bool IsAnonymous,
    string? PublicPassword
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
    string? PublicToken,
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

// Public (unauthenticated) voting
public record PublicDecisionInfoDto(string Title);

public record PublicOptionDto(int Id, string Text);

public record PublicVoteResultDto(int OptionId, int Count, List<string> VoterNames);

public record PublicDecisionDto(
    string Title,
    string? Considerations,
    bool AllowMultipleVotes,
    bool IsAnonymous,
    bool IsResolved,
    int? WinnerOptionId,
    string? WinnerText,
    List<PublicOptionDto> Options,
    List<PublicVoteResultDto> Results,
    List<int> MyVotedOptionIds
);

public record PublicPollRequest(string VoterName, string Password);

public record PublicVoteRequest(string VoterName, int OptionId, string Password);
