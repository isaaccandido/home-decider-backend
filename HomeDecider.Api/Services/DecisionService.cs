using HomeDecider.Api.Data;
using HomeDecider.Api.Dtos;
using HomeDecider.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HomeDecider.Api.Services;

public class DecisionService(AppDbContext db, IConfiguration config, ILogger<DecisionService> logger) : IDecisionService
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
