using MermaidSharp.Core.Models;
using MermaidSharp.Core.Rendering;

namespace MermaidSharp.Core;

public interface IDiagramRenderer<in TModel> where TModel : DiagramBase
{
    SvgDocument Render(TModel model, RenderOptions options);
}

public class RenderOptions
{
    public static RenderOptions Default => new();

    public double Padding { get; set; } = 20;
    public string Theme { get; set; } = "default";
    public double FontSize { get; set; } = 14;
    public string FontFamily { get; set; } = "Arial, sans-serif";
    public bool ShowBoundingBox { get; set; }
}
