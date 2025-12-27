namespace MermaidSharp;

public interface IDiagramRenderer<in TModel> where TModel : DiagramBase
{
    SvgDocument Render(TModel model, RenderOptions options);
}