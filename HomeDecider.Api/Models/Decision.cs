namespace HomeDecider.Api.Models;

public class Decision
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Considerations { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsResolved { get; set; } = false;
    public int? WinnerOptionId { get; set; }
    public bool AllowMultipleVotes { get; set; } = false;
    public bool IsAnonymous { get; set; } = false;

    public List<Option> Options { get; set; } = [];
    public List<Vote> Votes { get; set; } = [];
}
