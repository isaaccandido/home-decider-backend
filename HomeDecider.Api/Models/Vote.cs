namespace HomeDecider.Api.Models;

public class Vote
{
    public int Id { get; set; }
    public int DecisionId { get; set; }
    public int OptionId { get; set; }
    public string VoterName { get; set; } = string.Empty;
    public DateTime CastAt { get; set; } = DateTime.UtcNow;

    public Decision Decision { get; set; } = null!;
    public Option Option { get; set; } = null!;
}
