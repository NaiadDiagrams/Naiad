using System.Globalization;
using MermaidSharp;
using MermaidSharp.Rendering;

namespace MermaidSharp.Diagrams.Sankey;

public class SankeyRenderer : IDiagramRenderer<SankeyModel>
{
    const double NodeWidth = 20;
    const double NodePadding = 10;
    const double ColumnSpacing = 200;
    const double MinNodeHeight = 20;
    const double TitleHeight = 40;

    static readonly string[] NodeColors =
    [
        "#4CAF50", "#2196F3", "#FF9800", "#E91E63",
        "#9C27B0", "#00BCD4", "#FF5722", "#607D8B"
    ];

    public SvgDocument Render(SankeyModel model, RenderOptions options)
    {
        if (model.Links.Count == 0)
        {
            var emptyBuilder = new SvgBuilder().Size(200, 100);
            emptyBuilder.AddText(100, 50, "Empty diagram", anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily);
            return emptyBuilder.Build();
        }

        // Build node structure
        var nodes = BuildNodes(model);
        AssignColumns(nodes, model);

        // Calculate scale
        var maxColumn = nodes.Values.Max(n => n.Column);
        var totalValue = nodes.Values.Where(n => n.Column == 0).Sum(n => n.OutputValue);

        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : TitleHeight;
        var chartHeight = Math.Max(300, totalValue * 2);
        var chartWidth = (maxColumn + 1) * ColumnSpacing;

        var width = chartWidth + options.Padding * 2 + 100;
        var height = chartHeight + options.Padding * 2 + titleOffset;

        var builder = new SvgBuilder().Size(width, height);

        // Draw title
        if (!string.IsNullOrEmpty(model.Title))
        {
            builder.AddText(width / 2, options.Padding + TitleHeight / 2, model.Title,
                anchor: "middle",
                baseline: "middle",
                fontSize: $"{options.FontSize + 4}px",
                fontFamily: options.FontFamily,
                fontWeight: "bold");
        }

        // Position nodes
        PositionNodes(nodes, chartHeight, titleOffset + options.Padding);

        // Draw links first (behind nodes)
        foreach (var link in model.Links)
        {
            var sourceNode = nodes[link.Source];
            var targetNode = nodes[link.Target];

            var linkHeight = link.Value / Math.Max(1, sourceNode.OutputValue) * sourceNode.Height;
            var sourceY = GetLinkSourceY(sourceNode, link, model.Links, nodes);
            var targetY = GetLinkTargetY(targetNode, link, model.Links, nodes);

            var sourceX = options.Padding + sourceNode.Column * ColumnSpacing + NodeWidth;
            var targetX = options.Padding + targetNode.Column * ColumnSpacing;

            // Draw bezier curve for link
            var pathData = CreateLinkPath(sourceX, sourceY, targetX, targetY, linkHeight);
            var colorIndex = Array.IndexOf(nodes.Keys.ToArray(), link.Source) % NodeColors.Length;
            builder.AddPath(pathData,
                fill: NodeColors[colorIndex],
                stroke: "none");
        }

        // Draw nodes
        int nodeIndex = 0;
        foreach (var (name, node) in nodes)
        {
            var x = options.Padding + node.Column * ColumnSpacing;
            var color = NodeColors[nodeIndex % NodeColors.Length];

            builder.AddRect(x, node.Y, NodeWidth, node.Height,
                fill: color, stroke: "#333", strokeWidth: 1);

            // Node label
            var labelX = node.Column == maxColumn
                ? x + NodeWidth + 5
                : x - 5;
            var anchor = node.Column == maxColumn ? "start" : "end";

            builder.AddText(labelX, node.Y + node.Height / 2, name,
                anchor: anchor, baseline: "middle",
                fontSize: $"{options.FontSize - 1}px", fontFamily: options.FontFamily,
                fill: "#333");

            nodeIndex++;
        }

        return builder.Build();
    }

    static Dictionary<string, SankeyNode> BuildNodes(SankeyModel model)
    {
        var nodes = new Dictionary<string, SankeyNode>();

        foreach (var link in model.Links)
        {
            if (!nodes.ContainsKey(link.Source))
                nodes[link.Source] = new SankeyNode { Name = link.Source };
            if (!nodes.ContainsKey(link.Target))
                nodes[link.Target] = new SankeyNode { Name = link.Target };

            nodes[link.Source].OutputValue += link.Value;
            nodes[link.Target].InputValue += link.Value;
        }

        return nodes;
    }

    static void AssignColumns(Dictionary<string, SankeyNode> nodes, SankeyModel model)
    {
        // Find source nodes (no incoming links)
        var targets = model.Links.Select(l => l.Target).ToHashSet();
        var sources = model.Links.Select(l => l.Source).ToHashSet();

        var sourceOnly = sources.Except(targets).ToList();

        // BFS to assign columns
        var queue = new Queue<string>();
        foreach (var name in sourceOnly)
        {
            nodes[name].Column = 0;
            queue.Enqueue(name);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            var currentColumn = nodes[current].Column;

            foreach (var link in model.Links.Where(l => l.Source == current))
            {
                var targetNode = nodes[link.Target];
                if (targetNode.Column <= currentColumn)
                {
                    targetNode.Column = currentColumn + 1;
                    queue.Enqueue(link.Target);
                }
            }
        }
    }

    static void PositionNodes(Dictionary<string, SankeyNode> nodes, double chartHeight, double topOffset)
    {
        var maxColumn = nodes.Values.Max(n => n.Column);

        for (int col = 0; col <= maxColumn; col++)
        {
            var columnNodes = nodes.Values.Where(n => n.Column == col).ToList();
            var totalValue = columnNodes.Sum(n => Math.Max(n.InputValue, n.OutputValue));
            var scale = (chartHeight - (columnNodes.Count - 1) * NodePadding) / Math.Max(1, totalValue);

            double y = topOffset;
            foreach (var node in columnNodes)
            {
                var value = Math.Max(node.InputValue, node.OutputValue);
                node.Height = Math.Max(MinNodeHeight, value * scale);
                node.Y = y;
                y += node.Height + NodePadding;
            }
        }
    }

    static double GetLinkSourceY(SankeyNode sourceNode, SankeyLink link, List<SankeyLink> allLinks,
        Dictionary<string, SankeyNode> nodes)
    {
        var outgoingLinks = allLinks.Where(l => l.Source == link.Source).ToList();
        double offset = 0;
        foreach (var l in outgoingLinks)
        {
            if (l == link) break;
            offset += l.Value / sourceNode.OutputValue * sourceNode.Height;
        }
        var linkHeight = link.Value / sourceNode.OutputValue * sourceNode.Height;
        return sourceNode.Y + offset + linkHeight / 2;
    }

    static double GetLinkTargetY(SankeyNode targetNode, SankeyLink link, List<SankeyLink> allLinks,
        Dictionary<string, SankeyNode> nodes)
    {
        var incomingLinks = allLinks.Where(l => l.Target == link.Target).ToList();
        double offset = 0;
        foreach (var l in incomingLinks)
        {
            if (l == link) break;
            offset += l.Value / targetNode.InputValue * targetNode.Height;
        }
        var linkHeight = link.Value / targetNode.InputValue * targetNode.Height;
        return targetNode.Y + offset + linkHeight / 2;
    }

    static string CreateLinkPath(double x1, double y1, double x2, double y2, double height)
    {
        var halfHeight = height / 2;
        var cx = (x1 + x2) / 2;

        return $"M {Fmt(x1)} {Fmt(y1 - halfHeight)} " +
               $"C {Fmt(cx)} {Fmt(y1 - halfHeight)} {Fmt(cx)} {Fmt(y2 - halfHeight)} {Fmt(x2)} {Fmt(y2 - halfHeight)} " +
               $"L {Fmt(x2)} {Fmt(y2 + halfHeight)} " +
               $"C {Fmt(cx)} {Fmt(y2 + halfHeight)} {Fmt(cx)} {Fmt(y1 + halfHeight)} {Fmt(x1)} {Fmt(y1 + halfHeight)} " +
               $"Z";
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
