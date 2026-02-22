namespace MermaidSharp.Diagrams.GitGraph;

public class CheckoutOperation : GitOperation
{
    public required string BranchName { get; init; }
}