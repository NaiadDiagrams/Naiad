namespace MermaidSharp.Rendering;

public class SvgPolygon : SvgElement
{
    public List<Position> Points { get; } = [];
    public string? Fill { get; set; }
    public string? Stroke { get; set; }

    public override string ToXml()
    {
        var pointsStr = string.Join(' ', Points.Select(p => $"{Fmt(p.X)},{Fmt(p.Y)}"));
        var builder = new StringBuilder($"<polygon points=\"{pointsStr}\"");

        if (Fill is not null)
        {
            builder.Append($" fill=\"{Fill}\"");
        }

        if (Stroke is not null)
        {
            builder.Append($" stroke=\"{Stroke}\"");
        }

        builder.Append(CommonAttributes());
        builder.Append("/>");
        return builder.ToString();
    }
}