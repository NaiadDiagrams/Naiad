namespace MermaidSharp.Diagrams.GitGraph;

public class CommitOperation : GitOperation
{
    public string? Id { get; set; }
    public string? Message { get; set; }
    public string? Tag { get; set; }
    public CommitType Type { get; set; } = CommitType.Normal;
}