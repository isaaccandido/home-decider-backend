namespace HomeDecider.Api.Models;

public class Option
{
    public int Id { get; set; }
    public int DecisionId { get; set; }
    public string Text { get; set; } = string.Empty;

    public Decision Decision { get; set; } = null!;
    public List<Vote> Votes { get; set; } = [];
}
