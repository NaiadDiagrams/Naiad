namespace MermaidSharp.Diagrams.Block;

public class BlockRenderer : IDiagramRenderer<BlockModel>
{
    const double CellWidth = 120;
    const double CellHeight = 60;
    const double CellPadding = 10;
    const double TitleHeight = 40;

    static readonly string[] BlockColors =
    [
        "#E3F2FD", "#E8F5E9", "#FFF3E0", "#F3E5F5",
        "#FCE4EC", "#E0F7FA", "#FFF8E1", "#F1F8E9"
    ];

    public SvgDocument Render(BlockModel model, RenderOptions options)
    {
        if (model.Elements.Count == 0)
        {
            var emptyBuilder = new SvgBuilder().Size(200, 100);
            emptyBuilder.AddText(100, 50, "Empty diagram", anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily);
            return emptyBuilder.Build();
        }

        var columns = Math.Max(1, model.Columns);
        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : TitleHeight;

        // Calculate rows needed
        var rows = CalculateRows(model.Elements, columns);

        var width = columns * CellWidth + options.Padding * 2;
        var height = rows * CellHeight + options.Padding * 2 + titleOffset;

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

        // Position and draw elements
        int currentColumn = 0;
        int currentRow = 0;
        int colorIndex = 0;

        foreach (var element in model.Elements)
        {
            var span = Math.Min(element.Span, columns - currentColumn);
            if (span <= 0 || currentColumn + span > columns)
            {
                currentRow++;
                currentColumn = 0;
                span = Math.Min(element.Span, columns);
            }

            var x = options.Padding + currentColumn * CellWidth + CellPadding;
            var y = titleOffset + options.Padding + currentRow * CellHeight + CellPadding;
            var blockWidth = span * CellWidth - CellPadding * 2;
            var blockHeight = CellHeight - CellPadding * 2;

            var color = BlockColors[colorIndex % BlockColors.Length];
            var label = element.Label ?? element.Id;

            DrawBlock(builder, x, y, blockWidth, blockHeight, label, element.Shape, color, options);

            currentColumn += span;
            if (currentColumn >= columns)
            {
                currentColumn = 0;
                currentRow++;
            }
            colorIndex++;
        }

        return builder.Build();
    }

    static int CalculateRows(List<BlockElement> elements, int columns)
    {
        int currentColumn = 0;
        int rows = 1;

        foreach (var element in elements)
        {
            var span = Math.Min(element.Span, columns);
            if (currentColumn + span > columns)
            {
                rows++;
                currentColumn = span;
            }
            else
            {
                currentColumn += span;
            }

            if (currentColumn >= columns)
            {
                rows++;
                currentColumn = 0;
            }
        }

        return rows == 1 || currentColumn > 0 ? rows : rows - 1;
    }

    static void DrawBlock(SvgBuilder builder, double x, double y, double width, double height,
        string label, BlockShape shape, string color, RenderOptions options)
    {
        var centerX = x + width / 2;
        var centerY = y + height / 2;

        switch (shape)
        {
            case BlockShape.Rectangle:
                builder.AddRect(x, y, width, height, rx: 4,
                    fill: color, stroke: "#333", strokeWidth: 1);
                break;

            case BlockShape.Rounded:
                builder.AddRect(x, y, width, height, rx: height / 2,
                    fill: color, stroke: "#333", strokeWidth: 1);
                break;

            case BlockShape.Stadium:
                builder.AddRect(x, y, width, height, rx: height / 2,
                    fill: color, stroke: "#333", strokeWidth: 1);
                break;

            case BlockShape.Circle:
                var radius = Math.Min(width, height) / 2;
                builder.AddCircle(centerX, centerY, radius,
                    fill: color, stroke: "#333", strokeWidth: 1);
                break;

            case BlockShape.Diamond:
                var diamondPath = $"M {Fmt(centerX)} {Fmt(y)} " +
                    $"L {Fmt(x + width)} {Fmt(centerY)} " +
                    $"L {Fmt(centerX)} {Fmt(y + height)} " +
                    $"L {Fmt(x)} {Fmt(centerY)} Z";
                builder.AddPath(diamondPath, fill: color, stroke: "#333", strokeWidth: 1);
                break;

            case BlockShape.Hexagon:
                var hOffset = width * 0.15;
                var hexPath = $"M {Fmt(x + hOffset)} {Fmt(y)} " +
                    $"L {Fmt(x + width - hOffset)} {Fmt(y)} " +
                    $"L {Fmt(x + width)} {Fmt(centerY)} " +
                    $"L {Fmt(x + width - hOffset)} {Fmt(y + height)} " +
                    $"L {Fmt(x + hOffset)} {Fmt(y + height)} " +
                    $"L {Fmt(x)} {Fmt(centerY)} Z";
                builder.AddPath(hexPath, fill: color, stroke: "#333", strokeWidth: 1);
                break;
        }

        // Label
        builder.AddText(centerX, centerY, label,
            anchor: "middle", baseline: "middle",
            fontSize: $"{options.FontSize - 1}px", fontFamily: options.FontFamily,
            fill: "#333");
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
