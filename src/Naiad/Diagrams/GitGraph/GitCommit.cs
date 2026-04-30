namespace Naiad.Diagrams.GitGraph;

public class GitCommit
{
    public required string Id { get; init; }
    public string? Message { get; set; }
    public string? Tag { get; set; }
    public CommitType Type { get; set; } = CommitType.Normal;
    public required string Branch { get; init; }
    public List<string> Parents { get; } = [];

    // Layout properties
    public int Row { get; set; }
}