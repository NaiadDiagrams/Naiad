namespace MermaidSharp.Diagrams.GitGraph;

public class GitGraphModel : DiagramBase
{
    public List<GitOperation> Operations { get; } = [];
    public string MainBranchName { get; set; } = "main";
    public int MainBranchOrder { get; set; }
}