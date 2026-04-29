namespace Naiad.Diagrams.Flowchart;

public partial class FlowchartRenderer(ILayoutEngine? layoutEngine = null) :
    IDiagramRenderer<FlowchartModel>
{
    ILayoutEngine layoutEngine = layoutEngine ?? new DagreLayoutEngine();

    // Mermaid.ink default colors
    const string nodeFill = "#ECECFF";
    const string nodeStroke = "#9370DB";
    const string edgeStroke = "#333333";
    const string labelBackground = "rgba(232,232,232,0.8)";

    // FontAwesome icon pattern: fa:fa-icon-name or fab:fa-icon-name
    static Regex iconPattern = IconPatternMyRegex();
    [GeneratedRegex("(fa[bsr]?):fa-([a-z0-9-]+)", RegexOptions.Compiled)]
    private static partial Regex IconPatternMyRegex();

    public SvgDocument Render(FlowchartModel model, RenderOptions options)
    {
        // Calculate node sizes based on text
        foreach (var node in model.Nodes)
        {
            var label = node.Label ?? node.Id;
            // Strip icon syntax for measurement
            var textForMeasure = iconPattern.Replace(label, "").Trim();
            var textSize = MeasureText(textForMeasure, options.FontSize);
            // Add extra width for icon if present
            var hasIcon = iconPattern.IsMatch(label);
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
        var layoutResult = layoutEngine.Layout(model, layoutOptions);

        // Build SVG
        var builder = new SvgBuilder()
            .Size(layoutResult.Width, layoutResult.Height)
            .Padding(options.Padding)
            .AddMermaidArrowMarker()
            .AddMermaidCircleMarker()
            .AddMermaidCrossMarker();

        // Add mermaid.ink CSS styles
        builder.AddStyles(MermaidStyles.FlowchartStyles);

        // Render edges first (behind nodes)
        foreach (var edge in model.Edges)
        {
            RenderEdge(builder, edge);
        }

        // Render nodes
        foreach (var node in model.Nodes)
        {
            RenderNode(builder, node);
        }

        return builder.Build();
    }

    static void RenderNode(SvgBuilder builder, Node node)
    {
        var x = node.Position.X - node.Width / 2;
        var y = node.Position.Y - node.Height / 2;

        var shapePath = ShapePathGenerator.GetPath(node.Shape, x, y, node.Width, node.Height);

        builder.AddPath(shapePath,
            fill: nodeFill,
            stroke: nodeStroke,
            strokeWidth: 1);

        // Render label with icon support
        var label = node.Label ?? node.Id;
        var htmlLabel = ConvertIconsToHtml(label);

        builder.AddForeignObject(
            x, y, node.Width, node.Height,
            htmlLabel,
            className: "nodeLabel");
    }

    static void RenderEdge(SvgBuilder builder, Edge edge)
    {
        if (edge.Points.Count < 2)
        {
            return;
        }

        // Build path from points
        var points = edge.Points;
        var pathBuilder = new StringBuilder();
        pathBuilder.Append(CultureInfo.InvariantCulture, $"M{points[0].X:0.##},{points[0].Y:0.##}");
        for (var i = 1; i < points.Count; i++)
        {
            pathBuilder.Append(CultureInfo.InvariantCulture, $" L{points[i].X:0.##},{points[i].Y:0.##}");
        }

        var pathData = pathBuilder.ToString();

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

        builder.AddPath(
            pathData,
            fill: "none",
            stroke: edgeStroke,
            strokeWidth: strokeWidth,
            strokeDasharray: strokeDasharray,
            markerEnd: markerEnd,
            markerStart: markerStart,
            cssClass: "flowchart-link");

        // Render edge label if present
        if (!string.IsNullOrEmpty(edge.Label))
        {
            var labelX = edge.LabelPosition.X;
            var labelY = edge.LabelPosition.Y;
            var labelWidth = edge.Label.Length * 8 + 16;
            var labelHeight = 24;

            builder.AddRect(
                labelX - labelWidth / 2, labelY - labelHeight / 2,
                labelWidth, labelHeight,
                fill: labelBackground, stroke: "none",
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
        if (!iconPattern.IsMatch(text))
        {
            return $"<p>{System.Net.WebUtility.HtmlEncode(text)}</p>";
        }

        // Build HTML by processing text segments and icons
        var html = new StringBuilder();
        var lastIndex = 0;

        foreach (Match match in iconPattern.Matches(text))
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
        return new(width, height);
    }
}
