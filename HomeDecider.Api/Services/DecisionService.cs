using HomeDecider.Api.Data;
using HomeDecider.Api.Dtos;
using HomeDecider.Api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HomeDecider.Api.Services;

public class DecisionService(
    AppDbContext db,
    IConfiguration config,
    ILogger<DecisionService> logger,
    IPasswordHasher<Decision> passwordHasher) : IDecisionService
{
    public async Task<IEnumerable<DecisionSummaryDto>> GetAllAsync()
    {
        var decisions = await db.Decisions
            .Include(d => d.Options)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return decisions.Select(d => new DecisionSummaryDto(
            d.Id,
            d.Title,
            d.CreatedAt,
            d.IsResolved,
            d.WinnerOptionId.HasValue
                ? d.Options.FirstOrDefault(o => o.Id == d.WinnerOptionId)?.Text
                : null
        ));
    }

    public async Task<DecisionDto?> GetByIdAsync(int id)
    {
        var d = await db.Decisions
            .Include(d => d.Options)
            .Include(d => d.Votes).ThenInclude(v => v.Option)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (d is null) return null;

        return new DecisionDto(
            d.Id,
            d.Title,
            d.Considerations,
            d.CreatedAt,
            d.IsResolved,
            d.WinnerOptionId,
            d.AllowMultipleVotes,
            d.IsAnonymous,
            d.PublicToken,
            d.Options.Select(o => new OptionDto(
                o.Id, o.Text,
                d.Votes.Where(v => v.OptionId == o.Id).Select(v => v.VoterName).ToList()
            )).ToList(),
            d.Votes.Select(v => new VoteDto(v.VoterName, v.OptionId, v.Option.Text)).ToList()
        );
    }

    public async Task<(int? Id, string? Error)> CreateAsync(CreateDecisionRequest request, string actor)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            return (null, "Title is required.");
        if (request.Options is null || request.Options.Count < 2)
            return (null, "At least 2 options required.");

        var decision = new Decision
        {
            Title = request.Title.Trim(),
            Considerations = request.Considerations?.Trim(),
            AllowMultipleVotes = request.AllowMultipleVotes,
            IsAnonymous = request.IsAnonymous,
            Options = request.Options
                .Where(o => !string.IsNullOrWhiteSpace(o))
                .Select(o => new Option { Text = o.Trim() })
                .ToList()
        };

        if (!string.IsNullOrWhiteSpace(request.PublicPassword))
        {
            decision.PublicToken = Guid.NewGuid().ToString("N");
            decision.PublicPasswordHash = passwordHasher.HashPassword(decision, request.PublicPassword);
        }

        db.Decisions.Add(decision);
        await db.SaveChangesAsync();

        logger.LogInformation("Decision '{Title}' created by '{Actor}' with {Count} options.", decision.Title, actor, decision.Options.Count);
        return (decision.Id, null);
    }

    public async Task<(bool Success, string? Error)> CastVoteAsync(int decisionId, int optionId, string voterName)
    {
        var decision = await db.Decisions
            .Include(d => d.Options)
            .Include(d => d.Votes)
            .FirstOrDefaultAsync(d => d.Id == decisionId);

        if (decision is null) return (false, "Decision not found.");
        if (decision.IsResolved) return (false, "Decision is already resolved.");
        if (!decision.Options.Any(o => o.Id == optionId)) return (false, "Invalid option.");

        var optionText = decision.Options.First(o => o.Id == optionId).Text;

        if (decision.AllowMultipleVotes)
        {
            var existing = decision.Votes.FirstOrDefault(v => v.VoterName == voterName && v.OptionId == optionId);
            if (existing is not null)
            {
                db.Remove(existing);
                logger.LogInformation("'{Voter}' removed vote for '{Option}' on decision '{Title}'.", voterName, optionText, decision.Title);
            }
            else
            {
                decision.Votes.Add(new Vote { DecisionId = decisionId, OptionId = optionId, VoterName = voterName });
                logger.LogInformation("'{Voter}' voted for '{Option}' on decision '{Title}'.", voterName, optionText, decision.Title);
            }
        }
        else
        {
            var previous = decision.Votes.Where(v => v.VoterName == voterName).ToList();
            db.RemoveRange(previous);
            decision.Votes.Add(new Vote { DecisionId = decisionId, OptionId = optionId, VoterName = voterName });
            logger.LogInformation("'{Voter}' voted for '{Option}' on decision '{Title}'.", voterName, optionText, decision.Title);
        }

        await db.SaveChangesAsync();

        if (!decision.AllowMultipleVotes)
            await TryAutoResolveAsync(decision);

        return (true, null);
    }

    public async Task<(bool Success, string? Error)> ResolveAsync(int decisionId, int winnerOptionId, string actor)
    {
        var decision = await db.Decisions.Include(d => d.Options).FirstOrDefaultAsync(d => d.Id == decisionId);
        if (decision is null) return (false, "Decision not found.");
        if (!decision.Options.Any(o => o.Id == winnerOptionId)) return (false, "Invalid option.");

        var winnerText = decision.Options.First(o => o.Id == winnerOptionId).Text;
        decision.IsResolved = true;
        decision.WinnerOptionId = winnerOptionId;
        await db.SaveChangesAsync();

        logger.LogInformation("Decision '{Title}' manually resolved by '{Actor}' — winner: '{Winner}'.", decision.Title, actor, winnerText);
        return (true, null);
    }

    // ── Public (unauthenticated) voting ──────────────────────────────

    public async Task<PublicDecisionInfoDto?> GetPublicInfoAsync(string token)
    {
        var d = await db.Decisions.FirstOrDefaultAsync(d => d.PublicToken == token);
        if (d is null) return null;
        return new PublicDecisionInfoDto(d.Title);
    }

    public async Task<(PublicDecisionDto? Decision, string? Error)> GetPublicStateAsync(string token, string voterName, string password)
    {
        var d = await db.Decisions
            .Include(d => d.Options)
            .Include(d => d.Votes)
            .FirstOrDefaultAsync(d => d.PublicToken == token);

        if (d is null || d.PublicPasswordHash is null) return (null, "not_found");

        var result = passwordHasher.VerifyHashedPassword(d, d.PublicPasswordHash, password);
        if (result == PasswordVerificationResult.Failed) return (null, "wrong_password");

        return (BuildPublicDto(d, voterName), null);
    }

    public async Task<(PublicDecisionDto? Decision, string? Error)> CastPublicVoteAsync(string token, string voterName, int optionId, string password)
    {
        var d = await db.Decisions
            .Include(d => d.Options)
            .Include(d => d.Votes)
            .FirstOrDefaultAsync(d => d.PublicToken == token);

        if (d is null || d.PublicPasswordHash is null) return (null, "not_found");

        var result = passwordHasher.VerifyHashedPassword(d, d.PublicPasswordHash, password);
        if (result == PasswordVerificationResult.Failed) return (null, "wrong_password");

        if (d.IsResolved) return (null, "resolved");
        if (!d.Options.Any(o => o.Id == optionId)) return (null, "invalid_option");

        var optionText = d.Options.First(o => o.Id == optionId).Text;

        if (d.AllowMultipleVotes)
        {
            var existing = d.Votes.FirstOrDefault(v => v.VoterName == voterName && v.OptionId == optionId);
            if (existing is not null)
            {
                db.Remove(existing);
                logger.LogInformation("Public voter '{Voter}' removed vote for '{Option}' on '{Title}'.", voterName, optionText, d.Title);
            }
            else
            {
                d.Votes.Add(new Vote { DecisionId = d.Id, OptionId = optionId, VoterName = voterName });
                logger.LogInformation("Public voter '{Voter}' voted for '{Option}' on '{Title}'.", voterName, optionText, d.Title);
            }
        }
        else
        {
            var previous = d.Votes.Where(v => v.VoterName == voterName).ToList();
            db.RemoveRange(previous);
            d.Votes.Add(new Vote { DecisionId = d.Id, OptionId = optionId, VoterName = voterName });
            logger.LogInformation("Public voter '{Voter}' voted for '{Option}' on '{Title}'.", voterName, optionText, d.Title);
        }

        await db.SaveChangesAsync();
        await db.Entry(d).Collection(d => d.Votes).LoadAsync();

        return (BuildPublicDto(d, voterName), null);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static PublicDecisionDto BuildPublicDto(Decision d, string voterName)
    {
        var myVotedOptionIds = d.Votes
            .Where(v => v.VoterName == voterName)
            .Select(v => v.OptionId)
            .ToList();

        var results = d.Options.Select(o =>
        {
            var votes = d.Votes.Where(v => v.OptionId == o.Id).ToList();
            return new PublicVoteResultDto(
                o.Id,
                votes.Count,
                d.IsAnonymous ? [] : votes.Select(v => v.VoterName).ToList()
            );
        }).ToList();

        var winnerText = d.WinnerOptionId.HasValue
            ? d.Options.FirstOrDefault(o => o.Id == d.WinnerOptionId)?.Text
            : null;

        return new PublicDecisionDto(
            d.Title,
            d.Considerations,
            d.AllowMultipleVotes,
            d.IsAnonymous,
            d.IsResolved,
            d.WinnerOptionId,
            winnerText,
            d.Options.Select(o => new PublicOptionDto(o.Id, o.Text)).ToList(),
            results,
            myVotedOptionIds
        );
    }

    private async Task TryAutoResolveAsync(Decision decision)
    {
        await db.Entry(decision).Collection(d => d.Votes).LoadAsync();
        var voterNames = config["VoterNames"]?.Split(',') ?? ["Person 1", "Person 2"];
        var allVoted = voterNames.All(name => decision.Votes.Any(v => v.VoterName == name));

        if (!allVoted) return;

        var agreedOptionId = decision.Votes
            .GroupBy(v => v.OptionId)
            .Where(g => g.Count() == voterNames.Length)
            .Select(g => (int?)g.Key)
            .FirstOrDefault();

        if (agreedOptionId.HasValue)
        {
            decision.IsResolved = true;
            decision.WinnerOptionId = agreedOptionId;
            await db.SaveChangesAsync();
            var winnerText = decision.Options.First(o => o.Id == agreedOptionId).Text;
            logger.LogInformation("Decision '{Title}' auto-resolved — winner: '{Winner}'.", decision.Title, winnerText);
        }
        else
        {
            logger.LogInformation("Decision '{Title}' — all voted but no agreement reached.", decision.Title);
        }
    }
}
