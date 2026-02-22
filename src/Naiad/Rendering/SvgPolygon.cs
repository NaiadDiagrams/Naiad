namespace MermaidSharp.Rendering;

public class SvgPolygon : SvgElement
{
    public List<Position> Points { get; } = [];
    public string? Fill { get; set; }
    public string? Stroke { get; set; }

    public override void ToXml(StringBuilder builder)
    {
        builder.Append("<polygon points=\"");
        for (var i = 0; i < Points.Count; i++)
        {
            if (i > 0)
            {
                builder.Append(' ');
            }

            builder.Append(Fmt(Points[i].X));
            builder.Append(',');
            builder.Append(Fmt(Points[i].Y));
        }

        builder.Append('"');

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