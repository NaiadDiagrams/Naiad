namespace MermaidSharp.Rendering;

public class SvgGroup : SvgElement
{
    public List<SvgElement> Children { get; } = [];

    public override string ToXml()
    {
        var builder = new StringBuilder($"<g{CommonAttributes()}");

        if (Children.Count == 0)
        {
            builder.Append("/>");
        }
        else
        {
            builder.Append('>');
            foreach (var child in Children)
            {
                builder.Append(child.ToXml());
            }

            builder.Append("</g>");
        }

        return builder.ToString();
    }
}