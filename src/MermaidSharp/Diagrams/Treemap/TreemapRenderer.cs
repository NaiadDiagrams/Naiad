namespace MermaidSharp.Diagrams.Treemap;

public class TreemapRenderer : IDiagramRenderer<TreemapModel>
{
    const double TitleHeight = 30;
    const double DefaultWidth = 600;
    const double DefaultHeight = 400;

    static readonly string[] Colors =
    [
        "#8dd3c7", "#ffffb3", "#bebada", "#fb8072",
        "#80b1d3", "#fdb462", "#b3de69", "#fccde5",
        "#d9d9d9", "#bc80bd", "#ccebc5", "#ffed6f"
    ];

    public SvgDocument Render(TreemapModel model, RenderOptions options)
    {
        if (model.RootNodes.Count == 0)
        {
            var emptyBuilder = new SvgBuilder().Size(200, 100);
            emptyBuilder.AddText(100, 50, "Empty diagram", anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily);
            return emptyBuilder.Build();
        }

        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : TitleHeight;
        var chartWidth = DefaultWidth - options.Padding * 2;
        var chartHeight = DefaultHeight - options.Padding * 2 - titleOffset;

        var builder = new SvgBuilder().Size(DefaultWidth, DefaultHeight);

        // Draw title
        if (!string.IsNullOrEmpty(model.Title))
        {
            builder.AddText(DefaultWidth / 2, options.Padding + TitleHeight / 2, model.Title,
                anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize + 4}px", fontFamily: options.FontFamily,
                fontWeight: "bold");
        }

        // Calculate total value for layout
        var totalValue = model.RootNodes.Sum(n => n.TotalValue);
        if (totalValue <= 0) totalValue = 1;

        // Draw treemap using squarified layout
        var x = options.Padding;
        var y = options.Padding + titleOffset;
        var colorIndex = 0;

        DrawNodes(builder, model.RootNodes, x, y, chartWidth, chartHeight, 0, ref colorIndex, options);

        return builder.Build();
    }

    void DrawNodes(SvgBuilder builder, List<TreemapNode> nodes, double x, double y,
        double width, double height, int depth, ref int colorIndex, RenderOptions options)
    {
        if (nodes.Count == 0 || width <= 0 || height <= 0) return;

        var totalValue = nodes.Sum(n => n.TotalValue);
        if (totalValue <= 0) return;

        // Use slice-and-dice algorithm based on depth
        var horizontal = depth % 2 == 0;
        var currentPos = horizontal ? x : y;
        var totalSize = horizontal ? width : height;

        foreach (var node in nodes)
        {
            var ratio = node.TotalValue / totalValue;
            var nodeSize = totalSize * ratio;

            double nodeX, nodeY, nodeWidth, nodeHeight;
            if (horizontal)
            {
                nodeX = currentPos;
                nodeY = y;
                nodeWidth = nodeSize;
                nodeHeight = height;
                currentPos += nodeSize;
            }
            else
            {
                nodeX = x;
                nodeY = currentPos;
                nodeWidth = width;
                nodeHeight = nodeSize;
                currentPos += nodeSize;
            }

            if (nodeWidth < 2 || nodeHeight < 2) continue;

            var color = Colors[colorIndex % Colors.Length];
            colorIndex++;

            if (node.IsLeaf)
            {
                // Draw leaf rectangle
                DrawLeafNode(builder, node, nodeX, nodeY, nodeWidth, nodeHeight, color, options);
            }
            else
            {
                // Draw parent outline and recurse
                DrawParentNode(builder, node, nodeX, nodeY, nodeWidth, nodeHeight, color, depth, ref colorIndex, options);
            }
        }
    }

    void DrawLeafNode(SvgBuilder builder, TreemapNode node, double x, double y,
        double width, double height, string color, RenderOptions options)
    {
        builder.AddRect(x, y, width, height,
            fill: color, stroke: "#333", strokeWidth: 1);

        // Draw label if there's enough space
        if (width > 40 && height > 20)
        {
            var label = TruncateLabel(node.Name, width, options.FontSize);
            builder.AddText(x + width / 2, y + height / 2 - 6, label,
                anchor: "middle", baseline: "middle",
                fontSize: $"{Math.Min(options.FontSize, 12)}px", fontFamily: options.FontFamily,
                fill: "#333");

            if (node.Value.HasValue && height > 35)
            {
                builder.AddText(x + width / 2, y + height / 2 + 8, node.Value.Value.ToString("0.#"),
                    anchor: "middle", baseline: "middle",
                    fontSize: $"{Math.Min(options.FontSize - 2, 10)}px", fontFamily: options.FontFamily,
                    fill: "#666");
            }
        }
    }

    void DrawParentNode(SvgBuilder builder, TreemapNode node, double x, double y,
        double width, double height, string color, int depth, ref int colorIndex, RenderOptions options)
    {
        // Draw section header
        var headerHeight = Math.Min(20, height * 0.2);

        builder.AddRect(x, y, width, headerHeight,
            fill: DarkenColor(color, 0.2), stroke: "#333", strokeWidth: 1);

        if (width > 40)
        {
            var label = TruncateLabel(node.Name, width - 10, options.FontSize - 2);
            builder.AddText(x + 5, y + headerHeight / 2, label,
                anchor: "start", baseline: "middle",
                fontSize: $"{Math.Min(options.FontSize - 2, 10)}px", fontFamily: options.FontFamily,
                fill: "#333", fontWeight: "bold");
        }

        // Draw children in remaining space
        var childY = y + headerHeight;
        var childHeight = height - headerHeight;

        if (childHeight > 0)
        {
            DrawNodes(builder, node.Children, x, childY, width, childHeight, depth + 1, ref colorIndex, options);
        }
    }

    static string TruncateLabel(string text, double maxWidth, double fontSize)
    {
        var charWidth = fontSize * 0.6;
        var maxChars = (int)(maxWidth / charWidth);
        if (text.Length <= maxChars) return text;
        if (maxChars <= 3) return "";
        return text.Substring(0, maxChars - 3) + "...";
    }

    static string DarkenColor(string hexColor, double factor)
    {
        if (!hexColor.StartsWith("#") || hexColor.Length != 7)
            return hexColor;

        var r = (int)(Convert.ToInt32(hexColor.Substring(1, 2), 16) * (1 - factor));
        var g = (int)(Convert.ToInt32(hexColor.Substring(3, 2), 16) * (1 - factor));
        var b = (int)(Convert.ToInt32(hexColor.Substring(5, 2), 16) * (1 - factor));

        return $"#{r:X2}{g:X2}{b:X2}";
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
