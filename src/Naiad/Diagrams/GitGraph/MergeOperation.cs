namespace MermaidSharp.Diagrams.GitGraph;

public class MergeOperation : GitOperation
{
    public required string BranchName { get; init; }
    public string? Id { get; set; }
    public string? Tag { get; set; }
    public CommitType Type { get; set; } = CommitType.Normal;
}