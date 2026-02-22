namespace MermaidSharp.Diagrams.GitGraph;

public class CherryPickOperation : GitOperation
{
    public required string CommitId { get; init; }
    public string? Tag { get; set; }
}