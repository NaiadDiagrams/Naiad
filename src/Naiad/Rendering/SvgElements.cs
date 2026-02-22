namespace MermaidSharp.Rendering;

public abstract class SvgElement
{
    public string? Id { get; set; }
    public string? Class { get; set; }
    public string? Style { get; set; }
    public string? Transform { get; set; }

    public abstract string ToXml();

    protected string CommonAttributes()
    {
        var builder = new StringBuilder();

        if (Id is not null)
        {
            builder.Append($" id=\"{Id}\"");
        }

        if (Class is not null)
        {
            builder.Append($" class=\"{Class}\"");
        }

        if (Style is not null)
        {
            builder.Append($" style=\"{Style}\"");
        }

        if (Transform is not null)
        {
            builder.Append($" transform=\"{Transform}\"");
        }

        return builder.ToString();
    }

    protected static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}