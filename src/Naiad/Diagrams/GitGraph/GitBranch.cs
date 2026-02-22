namespace MermaidSharp.Diagrams.GitGraph;

public class GitBranch
{
    public required string Name { get; init; }
    public int Order { get; set; }
    public int Column { get; set; }
    public string? Color { get; set; }
    public List<GitCommit> Commits { get; } = [];
}