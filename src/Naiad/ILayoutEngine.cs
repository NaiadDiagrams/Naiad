namespace Naiad;

public interface ILayoutEngine
{
    LayoutResult Layout(GraphDiagramBase diagram, LayoutOptions options);
}