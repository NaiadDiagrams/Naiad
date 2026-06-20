using System.Net;

namespace Naiad.Diagrams.Flowchart;

public partial class FlowchartRenderer(ILayoutEngine? layoutEngine = null) :
    IDiagramRenderer<FlowchartModel>
{
    ILayoutEngine layoutEngine = layoutEngine ?? new DagreLayoutEngine();

    // Mermaid default colors
    const string nodeFill = "#ECECFF";
    const string nodeStroke = "#9370DB";
    const string edgeStroke = "#333333";
    const string labelBackground = "rgba(232,232,232,0.8)";
    const string subgraphFill = "#ffffff";
    const string subgraphStroke = "#bbbbbb";

    // Matches "prefix:name" icon tokens in labels — FontAwesome (fa:fa-bell) or a
    // registered iconify pack (logos:aws). Tokens that resolve to neither stay as text.
    static Regex iconPattern = IconPatternMyRegex();
    [GeneratedRegex("[A-Za-z0-9]+:[A-Za-z0-9][A-Za-z0-9_-]*", RegexOptions.Compiled)]
    private static partial Regex IconPatternMyRegex();

    public SvgDocument Render(FlowchartModel model, RenderOptions options)
    {
        // Calculate node sizes based on text
        foreach (var node in model.Nodes)
        {
            var label = node.Label ?? node.Id;
            var (textForMeasure, iconCount) = AnalyzeLabel(label);
            var textSize = MeasureText(textForMeasure, options.FontSize);
            node.Width = textSize.Width + 30 + iconCount * 20;
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
            .Options(options)
            .Size(layoutResult.Width, layoutResult.Height)
            .Padding(options.Padding);

        // The arrow/circle/cross markers are only referenced by edges; skip the defs entirely when there are none.
        if (model.Edges.Count > 0)
        {
            builder
                .AddMermaidArrowMarker()
                .AddMermaidCircleMarker()
                .AddMermaidCrossMarker();
        }

        // Add Mermaid CSS styles
        builder.AddStyles(MermaidStyles.FlowchartStyles);

        // Render subgraph boxes first (behind everything), outermost first.
        RenderSubgraphs(builder, model.Subgraphs, options);

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

    static void RenderSubgraphs(SvgBuilder builder, IEnumerable<Subgraph> subgraphs, RenderOptions options)
    {
        foreach (var subgraph in subgraphs)
        {
            RenderSubgraphBox(builder, subgraph, options);

            // Nested subgraphs after their parent so they sit on top of it.
            RenderSubgraphs(builder, subgraph.NestedSubgraphs, options);
        }
    }

    static void RenderSubgraphBox(SvgBuilder builder, Subgraph subgraph, RenderOptions options)
    {
        var bounds = subgraph.Bounds;

        builder.AddRect(
            bounds.X,
            bounds.Y,
            bounds.Width,
            bounds.Height,
            rx: 6,
            fill: subgraphFill,
            stroke: subgraphStroke,
            strokeWidth: 1);

        var title = subgraph.Title ?? subgraph.Id;
        if (!string.IsNullOrEmpty(title))
        {
            builder.AddText(
                subgraph.Position.X,
                bounds.Y + 14,
                title,
                anchor: "middle",
                baseline: "middle",
                fontSize: options.FontSize,
                fontFamily: options.FontFamily,
                fontWeight: "bold",
                fill: edgeStroke);
        }
    }

    static void RenderNode(SvgBuilder builder, Node node)
    {
        var x = node.Position.X - node.Width / 2;
        var y = node.Position.Y - node.Height / 2;

        var shapePath = ShapePathGenerator.GetPath(node.Shape, x, y, node.Width, node.Height);

        builder.AddPath(
            shapePath,
            fill: nodeFill,
            stroke: nodeStroke,
            strokeWidth: 1);

        // Render label with icon support
        var label = node.Label ?? node.Id;
        var htmlLabel = ConvertIconsToHtml(label);
        var (plainText, _) = AnalyzeLabel(label);

        builder.AddLabel(
            x, y,
            node.Width,
            node.Height,
            htmlLabel,
            plainText,
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

        var markerEnd = edge.HasArrowHead ? "url(#naiad_flowchart-pointEnd)" :
                        edge.HasCircleEnd ? "url(#naiad_flowchart-circleEnd)" :
                        edge.HasCrossEnd ? "url(#naiad_flowchart-crossEnd)" : null;

        var markerStart = edge.HasArrowTail ? "url(#naiad_flowchart-pointStart)" : null;

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
            const int labelHeight = 24;

            builder.AddRect(
                labelX - labelWidth / 2,
                labelY - labelHeight / 2,
                labelWidth, labelHeight,
                fill: labelBackground, stroke: "none",
                cssClass: "edgeLabel");

            builder.AddLabel(
                labelX - labelWidth / 2,
                labelY - labelHeight / 2,
                labelWidth, labelHeight,
                $"<p>{WebUtility.HtmlEncode(edge.Label)}</p>",
                edge.Label,
                className: "edgeLabel");
        }
    }

    /// <summary>
    /// Converts inline icon tokens within a label to HTML: FontAwesome
    /// (<c>fa:fa-name</c>) becomes an <c>&lt;i&gt;</c> element, a registered iconify
    /// pack icon (<c>prefix:name</c>) becomes inline SVG. Other text is HTML-encoded.
    /// </summary>
    static string ConvertIconsToHtml(string text)
    {
        var html = new StringBuilder("<p>");
        var lastIndex = 0;

        foreach (Match match in iconPattern.Matches(text))
        {
            if (IconTokenToHtml(match.Value) is not { } iconHtml)
            {
                continue;
            }

            if (match.Index > lastIndex)
            {
                html.Append(WebUtility.HtmlEncode(text[lastIndex..match.Index]));
            }

            html.Append(iconHtml);
            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < text.Length)
        {
            html.Append(WebUtility.HtmlEncode(text[lastIndex..]));
        }

        html.Append("</p>");
        return html.ToString();
    }

    // Removes recognised icon tokens from a label and counts them, for sizing.
    static (string text, int iconCount) AnalyzeLabel(string label)
    {
        var builder = new StringBuilder();
        var lastIndex = 0;
        var count = 0;

        foreach (Match match in iconPattern.Matches(label))
        {
            if (IconTokenToHtml(match.Value) is null)
            {
                continue;
            }

            builder.Append(label, lastIndex, match.Index - lastIndex);
            lastIndex = match.Index + match.Length;
            count++;
        }

        builder.Append(label, lastIndex, label.Length - lastIndex);
        return (builder.ToString(), count);
    }

    // Renders a single "prefix:name" token to inline HTML, or null if it is not an icon.
    static string? IconTokenToHtml(string token)
    {
        var colon = token.IndexOf(':');
        var prefix = token[..colon];
        var name = token[(colon + 1)..];

        // FontAwesome: fa/fab/fas/far + fa-name. Prefer registered geometry — inline SVG the PNG
        // rasterizer can actually draw — and fall back to the webfont <i> (which only resolves in a
        // browser with the Font Awesome CSS) when no matching pack is registered.
        if (prefix is "fa" or "fab" or "fas" or "far" &&
            name.StartsWith("fa-", StringComparison.Ordinal))
        {
            return IconPackRegistry.Resolve(token) is { } faIcon
                ? InlineIconSvg(faIcon)
                : $"<i class='{prefix} {name}'></i>";
        }

        // Registered iconify pack icon, rendered as inline SVG sized to the text.
        if (IconPackRegistry.Resolve(token) is { } icon)
        {
            return InlineIconSvg(icon);
        }

        return null;
    }

    static string InlineIconSvg(IconPackRegistry.PackIcon icon) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 {icon.Width:0.##} {icon.Height:0.##}' style='width:1em;height:1em;vertical-align:-0.125em'>{icon.Body}</svg>");

    static Size MeasureText(CharSpan text, double fontSize)
    {
        var width = text.Trim().Length * fontSize * 0.55;
        var height = fontSize * 1.5;
        return new(width, height);
    }
}
