namespace MermaidSharp.Diagrams.GitGraph;

public class ComputedGitGraph
{
    public List<GitBranch> Branches { get; } = [];
    public List<GitCommit> Commits { get; } = [];
    public Dictionary<string, GitCommit> CommitMap { get; } = [];
}