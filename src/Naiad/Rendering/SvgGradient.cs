namespace Naiad.Rendering;

public class SvgGradient
{
    public required string Id { get; init; }
    public List<SvgGradientStop> Stops { get; } = [];
    public bool IsRadial { get; set; }

    public void ToXml(StringBuilder builder)
    {
        var tag = IsRadial ? "radialGradient" : "linearGradient";
        builder.Append($"<{tag} id=\"{Id}\">");

        foreach (var stop in Stops)
        {
            builder.Append(CultureInfo.InvariantCulture, $"<stop offset=\"{stop.Offset}%\" style=\"stop-color:{stop.Color}\" />");
        }

        builder.Append($"</{tag}>");
    }
}