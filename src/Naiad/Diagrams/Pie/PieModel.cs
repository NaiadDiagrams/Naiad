namespace MermaidSharp.Diagrams.Pie;

public class PieModel : DiagramBase
{
    public bool ShowData { get; set; }
    public List<PieSection> Sections { get; } = [];
}