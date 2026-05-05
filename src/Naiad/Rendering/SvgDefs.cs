namespace Naiad.Rendering;

public class SvgDefs
{
    public List<SvgMarker> Markers { get; } = [];
    public List<SvgGradient> Gradients { get; } = [];
    public List<SvgFilter> Filters { get; } = [];

    public bool HasContent => Markers.Count > 0 || Gradients.Count > 0 || Filters.Count > 0;

    public void ToXml(StringBuilder builder)
    {
        builder.Append("<defs>");

        foreach (var marker in Markers)
        {
            marker.ToXml(builder);
        }

        foreach (var gradient in Gradients)
        {
            gradient.ToXml(builder);
        }

        foreach (var filter in Filters)
        {
            filter.ToXml(builder);
        }

        builder.Append("</defs>");
    }
}