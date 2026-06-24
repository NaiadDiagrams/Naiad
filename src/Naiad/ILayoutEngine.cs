namespace Naiad;

public interface ILayoutEngine
{
    LayoutResult BuildLayout(GraphDiagramBase diagram, LayoutOptions options);
}