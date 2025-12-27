using MermaidSharp.Layout;

namespace MermaidSharp.Diagrams.Flowchart;

public class FlowchartRenderer : IDiagramRenderer<FlowchartModel>
{
    readonly ILayoutEngine _layoutEngine;

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
            var textSize = MeasureText(label, options.FontSize);
            node.Width = textSize.Width + 20;
            node.Height = textSize.Height + 16;

            // Adjust size for different shapes
            if (node.Shape is NodeShape.Circle or NodeShape.DoubleCircle)
            {
                var diameter = Math.Max(node.Width, node.Height);
                node.Width = diameter;
                node.Height = diameter;
            }
            else if (node.Shape == NodeShape.Diamond)
            {
                node.Width *= 1.3;
                node.Height *= 1.3;
            }
        }

        // Run layout
        var layoutOptions = new LayoutOptions
        {
            Direction = model.Direction,
            NodeSeparation = 50,
            RankSeparation = 50
        };
        var layoutResult = _layoutEngine.Layout(model, layoutOptions);

        // Build SVG
        var width = layoutResult.Width + options.Padding * 2;
        var height = layoutResult.Height + options.Padding * 2;

        var builder = new SvgBuilder()
            .Size(width, height)
            .AddArrowMarker()
            .AddCircleMarker()
            .AddCrossMarker();

        // Add CSS styles
        builder.AddStyles(GetStyles(options));

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
        var x = node.Position.X - node.Width / 2;
        var y = node.Position.Y - node.Height / 2;
        var cx = node.Position.X;
        var cy = node.Position.Y;

        var shapePath = ShapePathGenerator.GetPath(node.Shape, x, y, node.Width, node.Height);

        builder.AddPath(shapePath,
            fill: "#f9f9f9",
            stroke: "#333",
            strokeWidth: 1.5);

        // Render label
        var label = node.Label ?? node.Id;
        builder.AddText(cx, cy, label,
            anchor: "middle",
            baseline: "central",
            fontSize: $"{options.FontSize}px",
            fontFamily: options.FontFamily);
    }

    void RenderEdge(SvgBuilder builder, Edge edge, RenderOptions options)
    {
        if (edge.Points.Count < 2) return;

        // Build path from points
        var points = edge.Points;
        var pathData = $"M{Fmt(points[0].X)},{Fmt(points[0].Y)}";

        if (points.Count == 2)
        {
            pathData += $" L{Fmt(points[1].X)},{Fmt(points[1].Y)}";
        }
        else
        {
            // Use curve for smoother edges
            for (int i = 1; i < points.Count; i++)
            {
                pathData += $" L{Fmt(points[i].X)},{Fmt(points[i].Y)}";
            }
        }

        var strokeDasharray = edge.LineStyle switch
        {
            EdgeStyle.Dotted => "5,5",
            _ => null
        };

        var strokeWidth = edge.LineStyle switch
        {
            EdgeStyle.Thick => 3.0,
            _ => 1.5
        };

        var markerEnd = edge.HasArrowHead ? "url(#arrowhead)" :
                        edge.HasCircleEnd ? "url(#circle)" :
                        edge.HasCrossEnd ? "url(#cross)" : null;

        var markerStart = edge.HasArrowTail ? "url(#arrowhead)" : null;

        builder.AddPath(pathData,
            fill: "none",
            stroke: "#333",
            strokeWidth: strokeWidth,
            strokeDasharray: strokeDasharray,
            markerEnd: markerEnd,
            markerStart: markerStart);

        // Render edge label if present
        if (!string.IsNullOrEmpty(edge.Label))
        {
            var labelPos = edge.LabelPosition;
            builder.AddRect(
                labelPos.X - 20, labelPos.Y - 10, 40, 20,
                fill: "#fff", stroke: "none");
            builder.AddText(labelPos.X, labelPos.Y, edge.Label,
                anchor: "middle",
                baseline: "central",
                fontSize: $"{options.FontSize - 2}px",
                fontFamily: options.FontFamily);
        }
    }

    static Size MeasureText(string text, double fontSize)
    {
        var width = text.Length * fontSize * 0.6;
        var height = fontSize * 1.4;
        return new Size(width, height);
    }

    static string GetStyles(RenderOptions options) =>
        ".node rect, .node circle, .node ellipse, .node polygon, .node path { " +
        "fill: #f9f9f9; stroke: #333; stroke-width: 1.5px; } " +
        $".node text {{ font-family: {options.FontFamily}; font-size: {options.FontSize}px; }} " +
        ".edge path { fill: none; stroke: #333; stroke-width: 1.5px; }";

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
