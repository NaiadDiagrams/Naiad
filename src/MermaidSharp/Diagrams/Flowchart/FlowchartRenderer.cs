using System.Text.RegularExpressions;
using MermaidSharp.Layout;

namespace MermaidSharp.Diagrams.Flowchart;

public class FlowchartRenderer : IDiagramRenderer<FlowchartModel>
{
    readonly ILayoutEngine _layoutEngine;

    // Mermaid.ink default colors
    const string NodeFill = "#ECECFF";
    const string NodeStroke = "#9370DB";
    const string EdgeStroke = "#333333";
    const string LabelBackground = "rgba(232,232,232,0.8)";

    // FontAwesome icon pattern: fa:fa-icon-name or fab:fa-icon-name
    static readonly Regex IconPattern = new("(fa[bsr]?):fa-([a-z0-9-]+)", RegexOptions.Compiled);

    public FlowchartRenderer(ILayoutEngine? layoutEngine = null)
    {
        _layoutEngine = layoutEngine ?? new DagreLayoutEngine();
    }

    public SvgDocument Render(FlowchartModel model, RenderOptions options)
    {
        // Calculate node sizes based on text
        foreach (var node in model.Nodes)
        {
            var label = node.Label ?? node.Id;
            // Strip icon syntax for measurement
            var textForMeasure = IconPattern.Replace(label, "").Trim();
            var textSize = MeasureText(textForMeasure, options.FontSize);
            // Add extra width for icon if present
            var hasIcon = IconPattern.IsMatch(label);
            node.Width = textSize.Width + 30 + (hasIcon ? 20 : 0);
            node.Height = textSize.Height + 27;

            // Adjust size for different shapes
            if (node.Shape is NodeShape.Circle or NodeShape.DoubleCircle)
            {
                var diameter = Math.Max(node.Width, node.Height);
                node.Width = diameter;
                node.Height = diameter;
            }
            else if (node.Shape == NodeShape.Diamond)
            {
                node.Width *= 1.4;
                node.Height *= 1.4;
            }
        }

        // Run layout
        var layoutOptions = new LayoutOptions
        {
            Direction = model.Direction,
            NodeSeparation = 50,
            RankSeparation = 70
        };
        var layoutResult = _layoutEngine.Layout(model, layoutOptions);

        // Build SVG
        var width = layoutResult.Width + options.Padding * 2;
        var height = layoutResult.Height + options.Padding * 2;

        var builder = new SvgBuilder()
            .Size(width, height)
            .AddMermaidArrowMarker()
            .AddMermaidCircleMarker()
            .AddMermaidCrossMarker();

        // Add mermaid.ink CSS styles
        builder.AddStyles(MermaidStyles.FlowchartStyles);

        // Render edges first (behind nodes)
        foreach (var edge in model.Edges)
        {
            RenderEdge(builder, edge, options);
        }

        // Render nodes
        foreach (var node in model.Nodes)
        {
            RenderNode(builder, node, options);
        }

        return builder.Build();
    }

    void RenderNode(SvgBuilder builder, Node node, RenderOptions options)
    {
        var x = node.Position.X - node.Width / 2 + options.Padding;
        var y = node.Position.Y - node.Height / 2 + options.Padding;
        var cx = node.Position.X + options.Padding;
        var cy = node.Position.Y + options.Padding;

        var shapePath = ShapePathGenerator.GetPath(node.Shape, x, y, node.Width, node.Height);

        builder.AddPath(shapePath,
            fill: NodeFill,
            stroke: NodeStroke,
            strokeWidth: 1);

        // Render label with icon support
        var label = node.Label ?? node.Id;
        var htmlLabel = ConvertIconsToHtml(label);

        builder.AddForeignObject(
            x, y, node.Width, node.Height,
            htmlLabel,
            className: "nodeLabel");
    }

    void RenderEdge(SvgBuilder builder, Edge edge, RenderOptions options)
    {
        if (edge.Points.Count < 2) return;

        // Build path from points, offset by padding
        var points = edge.Points;
        var pathData = $"M{Fmt(points[0].X + options.Padding)},{Fmt(points[0].Y + options.Padding)}";

        if (points.Count == 2)
        {
            pathData += $" L{Fmt(points[1].X + options.Padding)},{Fmt(points[1].Y + options.Padding)}";
        }
        else
        {
            // Use curve for smoother edges
            for (int i = 1; i < points.Count; i++)
            {
                pathData += $" L{Fmt(points[i].X + options.Padding)},{Fmt(points[i].Y + options.Padding)}";
            }
        }

        var strokeDasharray = edge.LineStyle switch
        {
            EdgeStyle.Dotted => "2",
            _ => null
        };

        var strokeWidth = edge.LineStyle switch
        {
            EdgeStyle.Thick => 3.5,
            _ => 2.0
        };

        var markerEnd = edge.HasArrowHead ? "url(#mermaid-svg_flowchart-v2-pointEnd)" :
                        edge.HasCircleEnd ? "url(#mermaid-svg_flowchart-v2-circleEnd)" :
                        edge.HasCrossEnd ? "url(#mermaid-svg_flowchart-v2-crossEnd)" : null;

        var markerStart = edge.HasArrowTail ? "url(#mermaid-svg_flowchart-v2-pointStart)" : null;

        builder.AddPath(pathData,
            fill: "none",
            stroke: EdgeStroke,
            strokeWidth: strokeWidth,
            strokeDasharray: strokeDasharray,
            markerEnd: markerEnd,
            markerStart: markerStart,
            cssClass: "flowchart-link");

        // Render edge label if present
        if (!string.IsNullOrEmpty(edge.Label))
        {
            var labelX = edge.LabelPosition.X + options.Padding;
            var labelY = edge.LabelPosition.Y + options.Padding;
            var labelWidth = edge.Label.Length * 8 + 16;
            var labelHeight = 24;

            builder.AddRect(
                labelX - labelWidth / 2, labelY - labelHeight / 2,
                labelWidth, labelHeight,
                fill: LabelBackground, stroke: "none",
                cssClass: "edgeLabel");

            builder.AddForeignObject(
                labelX - labelWidth / 2, labelY - labelHeight / 2,
                labelWidth, labelHeight,
                $"<p>{System.Net.WebUtility.HtmlEncode(edge.Label)}</p>",
                className: "edgeLabel");
        }
    }

    /// <summary>
    /// Converts FontAwesome icon syntax (fa:fa-icon) to HTML elements.
    /// </summary>
    static string ConvertIconsToHtml(string text)
    {
        // If no icons, just encode and wrap in paragraph
        if (!IconPattern.IsMatch(text))
        {
            return $"<p>{System.Net.WebUtility.HtmlEncode(text)}</p>";
        }

        // Build HTML by processing text segments and icons
        var html = new StringBuilder();
        var lastIndex = 0;

        foreach (Match match in IconPattern.Matches(text))
        {
            // Add text before this icon (encoded)
            if (match.Index > lastIndex)
            {
                var textBefore = text[lastIndex..match.Index];
                html.Append(System.Net.WebUtility.HtmlEncode(textBefore));
            }

            // Add the icon element
            var prefix = match.Groups[1].Value;
            var iconName = match.Groups[2].Value;
            html.Append($"<i class=\"{prefix} fa-{iconName}\"></i>");

            lastIndex = match.Index + match.Length;
        }

        // Add remaining text after last icon
        if (lastIndex < text.Length)
        {
            html.Append(System.Net.WebUtility.HtmlEncode(text[lastIndex..]));
        }

        return $"<p>{html.ToString().Trim()}</p>";
    }

    static Size MeasureText(string text, double fontSize)
    {
        var width = text.Length * fontSize * 0.55;
        var height = fontSize * 1.5;
        return new Size(width, height);
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
