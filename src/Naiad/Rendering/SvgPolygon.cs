namespace MermaidSharp.Rendering;

public class SvgPolygon : SvgElement
{
    public List<Position> Points { get; } = [];
    public string? Fill { get; set; }
    public string? Stroke { get; set; }

    public override void ToXml(StringBuilder builder)
    {
        var pointsStr = string.Join(' ', Points.Select(_ => $"{Fmt(_.X)},{Fmt(_.Y)}"));
        builder.Append($"<polygon points=\"{pointsStr}\"");

        if (Fill is not null)
        {
            builder.Append($" fill=\"{Fill}\"");
        }

        if (Stroke is not null)
        {
            builder.Append($" stroke=\"{Stroke}\"");
        }

        CommonAttributes(builder);
        builder.Append("/>");
    }
}