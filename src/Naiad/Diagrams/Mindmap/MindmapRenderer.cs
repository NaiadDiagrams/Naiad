namespace MermaidSharp.Diagrams.Mindmap;

public class MindmapRenderer : IDiagramRenderer<MindmapModel>
{
    const double NodePadding = 15;
    const double NodeMinWidth = 60;
    const double NodeHeight = 35;
    const double HorizontalSpacing = 40;
    const double VerticalSpacing = 15;

    static readonly string[] LevelColors =
    [
        "#FFB6C1", // pink - root
        "#87CEEB", // sky blue - level 1
        "#98FB98", // pale green - level 2
        "#DDA0DD", // plum - level 3
        "#F0E68C", // khaki - level 4
        "#E0FFFF", // light cyan - level 5
        "#FFDAB9"  // peach - level 6+
    ];

    public SvgDocument Render(MindmapModel model, RenderOptions options)
    {
        if (model.Root == null)
        {
            var emptyBuilder = new SvgBuilder().Size(200, 100);
            emptyBuilder.AddText(100, 50, "Empty mindmap", anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily);
            return emptyBuilder.Build();
        }

        // Calculate node sizes
        CalculateNodeSizes(model.Root, options);

        // Calculate subtree heights
        CalculateSubtreeHeights(model.Root);

        // Layout the tree
        LayoutTree(model.Root, options.Padding, options.Padding);

        // Calculate total dimensions
        var (width, height) = CalculateBounds(model.Root);
        width += options.Padding * 2;
        height += options.Padding * 2;

        var builder = new SvgBuilder().Size(width, height);

        // Draw connections first (behind nodes)
        DrawConnections(builder, model.Root);

        // Draw nodes
        DrawNodes(builder, model.Root, options);

        return builder.Build();
    }

    static void CalculateNodeSizes(MindmapNode node, RenderOptions options)
    {
        var textWidth = MeasureText(node.Text, options.FontSize);
        node.Width = Math.Max(NodeMinWidth, textWidth + NodePadding * 2);
        node.Height = NodeHeight;

        foreach (var child in node.Children)
        {
            CalculateNodeSizes(child, options);
        }
    }

    static double CalculateSubtreeHeights(MindmapNode node)
    {
        if (node.Children.Count == 0)
        {
            node.SubtreeHeight = node.Height;
            return node.SubtreeHeight;
        }

        double totalChildrenHeight = 0;
        foreach (var child in node.Children)
        {
            totalChildrenHeight += CalculateSubtreeHeights(child);
        }
        totalChildrenHeight += (node.Children.Count - 1) * VerticalSpacing;

        node.SubtreeHeight = Math.Max(node.Height, totalChildrenHeight);
        return node.SubtreeHeight;
    }

    static void LayoutTree(MindmapNode node, double x, double y)
    {
        // Position this node
        node.Position = new(x + node.Width / 2, y + node.SubtreeHeight / 2);

        if (node.Children.Count == 0)
            return;

        // Position children
        var childX = x + node.Width + HorizontalSpacing;
        var childY = y + (node.SubtreeHeight - GetChildrenTotalHeight(node)) / 2;

        foreach (var child in node.Children)
        {
            LayoutTree(child, childX, childY);
            childY += child.SubtreeHeight + VerticalSpacing;
        }
    }

    static double GetChildrenTotalHeight(MindmapNode node)
    {
        if (node.Children.Count == 0)
            return 0;

        return node.Children.Sum(c => c.SubtreeHeight) + (node.Children.Count - 1) * VerticalSpacing;
    }

    static (double width, double height) CalculateBounds(MindmapNode node)
    {
        var rightEdge = node.Position.X + node.Width / 2;
        var bottomEdge = node.Position.Y + node.Height / 2;

        foreach (var child in node.Children)
        {
            var (childRight, childBottom) = CalculateBounds(child);
            rightEdge = Math.Max(rightEdge, childRight);
            bottomEdge = Math.Max(bottomEdge, childBottom);
        }

        return (rightEdge, bottomEdge);
    }

    static void DrawConnections(SvgBuilder builder, MindmapNode node)
    {
        foreach (var child in node.Children)
        {
            // Draw curved connection from parent to child
            var startX = node.Position.X + node.Width / 2;
            var startY = node.Position.Y;
            var endX = child.Position.X - child.Width / 2;
            var endY = child.Position.Y;

            var controlX1 = startX + HorizontalSpacing / 2;
            var controlX2 = endX - HorizontalSpacing / 2;

            var path = $"M {Fmt(startX)} {Fmt(startY)} " +
                       $"C {Fmt(controlX1)} {Fmt(startY)}, {Fmt(controlX2)} {Fmt(endY)}, {Fmt(endX)} {Fmt(endY)}";

            var color = GetLevelColor(child.Level);
            builder.AddPath(path, stroke: color, strokeWidth: 2, fill: "none");

            // Recursively draw children's connections
            DrawConnections(builder, child);
        }
    }

    static void DrawNodes(SvgBuilder builder, MindmapNode node, RenderOptions options)
    {
        var x = node.Position.X - node.Width / 2;
        var y = node.Position.Y - node.Height / 2;
        var color = GetLevelColor(node.Level);
        var strokeColor = DarkenColor(color);

        switch (node.Shape)
        {
            case MindmapShape.Circle:
                var radius = Math.Max(node.Width, node.Height) / 2;
                builder.AddCircle(node.Position.X, node.Position.Y, radius,
                    fill: color, stroke: strokeColor, strokeWidth: 2);
                break;

            case MindmapShape.Square:
                builder.AddRect(x, y, node.Width, node.Height,
                    fill: color, stroke: strokeColor, strokeWidth: 2);
                break;

            case MindmapShape.Hexagon:
                DrawHexagon(builder, node.Position.X, node.Position.Y, node.Width, node.Height,
                    color, strokeColor);
                break;

            case MindmapShape.Cloud:
            case MindmapShape.Bang:
                // Simplified: draw as rounded rect with extra styling
                builder.AddRect(x, y, node.Width, node.Height,
                    rx: node.Height / 2,
                    fill: color, stroke: strokeColor, strokeWidth: 3);
                break;

            case MindmapShape.Rounded:
            case MindmapShape.Default:
            default:
                builder.AddRect(x, y, node.Width, node.Height,
                    rx: 8,
                    fill: color, stroke: strokeColor, strokeWidth: 2);
                break;
        }

        // Draw text
        builder.AddText(node.Position.X, node.Position.Y, node.Text,
            anchor: "middle",
            baseline: "middle",
            fontSize: $"{options.FontSize}px",
            fontFamily: options.FontFamily,
            fontWeight: node.Level == 0 ? "bold" : "normal");

        // Draw children
        foreach (var child in node.Children)
        {
            DrawNodes(builder, child, options);
        }
    }

    static void DrawHexagon(SvgBuilder builder, double cx, double cy, double width, double height,
        string fill, string stroke)
    {
        var hw = width / 2;
        var hh = height / 2;
        var offset = height / 4;

        var points = new[]
        {
            (cx - hw + offset, cy - hh),
            (cx + hw - offset, cy - hh),
            (cx + hw, cy),
            (cx + hw - offset, cy + hh),
            (cx - hw + offset, cy + hh),
            (cx - hw, cy)
        };

        var path = $"M {Fmt(points[0].Item1)} {Fmt(points[0].Item2)} " +
                   string.Join(" ", points.Skip(1).Select(p => $"L {Fmt(p.Item1)} {Fmt(p.Item2)}")) +
                   " Z";

        builder.AddPath(path, fill: fill, stroke: stroke, strokeWidth: 2);
    }

    static string GetLevelColor(int level) =>
        LevelColors[Math.Min(level, LevelColors.Length - 1)];

    static string DarkenColor(string hexColor)
    {
        // Simple darkening: reduce each component by 20%
        if (!hexColor.StartsWith('#') || hexColor.Length != 7)
            return "#333";

        var r = Convert.ToInt32(hexColor.Substring(1, 2), 16);
        var g = Convert.ToInt32(hexColor.Substring(3, 2), 16);
        var b = Convert.ToInt32(hexColor.Substring(5, 2), 16);

        r = (int)(r * 0.7);
        g = (int)(g * 0.7);
        b = (int)(b * 0.7);

        return $"#{r:X2}{g:X2}{b:X2}";
    }

    static double MeasureText(string text, double fontSize) =>
        text.Length * fontSize * 0.55;

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
