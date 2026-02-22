namespace MermaidSharp.Rendering;

public class SvgGradient
{
    public required string Id { get; init; }
    public List<SvgGradientStop> Stops { get; } = [];
    public bool IsRadial { get; set; }

    public string ToXml()
    {
        var tag = IsRadial ? "radialGradient" : "linearGradient";
        var builder = new StringBuilder($"<{tag} id=\"{Id}\">");

        foreach (var stop in Stops)
        {
            builder.Append($"<stop offset=\"{stop.Offset}%\" style=\"stop-color:{stop.Color}\" />");
        }

        builder.Append($"</{tag}>");
        return builder.ToString();
    }
}