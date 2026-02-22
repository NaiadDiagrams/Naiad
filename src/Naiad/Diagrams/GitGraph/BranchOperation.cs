namespace MermaidSharp.Diagrams.GitGraph;

public class BranchOperation : GitOperation
{
    public required string Name { get; init; }
    public int? BranchOrder { get; set; }
}