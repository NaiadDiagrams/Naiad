namespace MermaidSharp.Rendering;

public class SvgGradient
{
    public required string Id { get; init; }
    public List<SvgGradientStop> Stops { get; } = [];
    public bool IsRadial { get; set; }

    public string ToXml()
    {
        var tag = IsRadial ? "radialGradient" : "linearGradient";
        var sb = new StringBuilder();
        sb.Append($"<{tag} id=\"{Id}\">");
        foreach (var stop in Stops)
        {
            sb.Append($"<stop offset=\"{stop.Offset}%\" style=\"stop-color:{stop.Color}\" />");
        }
        sb.Append($"</{tag}>");
        return sb.ToString();
    }
}