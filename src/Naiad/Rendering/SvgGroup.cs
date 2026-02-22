namespace MermaidSharp.Rendering;

public class SvgGroup : SvgElement
{
    public List<SvgElement> Children { get; } = [];

    public override void ToXml(StringBuilder builder)
    {
        builder.Append("<g");
        CommonAttributes(builder);

        if (Children.Count == 0)
        {
            builder.Append("/>");
        }
        else
        {
            builder.Append('>');
            foreach (var child in Children)
            {
                child.ToXml(builder);
            }

            builder.Append("</g>");
        }
    }
}