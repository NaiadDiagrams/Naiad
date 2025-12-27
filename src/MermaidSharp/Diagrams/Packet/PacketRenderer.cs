namespace MermaidSharp.Diagrams.Packet;

public class PacketRenderer : IDiagramRenderer<PacketModel>
{
    const double BitWidth = 20;
    const double RowHeight = 40;
    const double BitNumberHeight = 20;
    const double TitleHeight = 40;

    static readonly string[] FieldColors =
    [
        "#E3F2FD", "#E8F5E9", "#FFF3E0", "#F3E5F5",
        "#FCE4EC", "#E0F7FA", "#FFF8E1", "#F1F8E9"
    ];

    public SvgDocument Render(PacketModel model, RenderOptions options)
    {
        if (model.Fields.Count == 0)
        {
            var emptyBuilder = new SvgBuilder().Size(200, 100);
            emptyBuilder.AddText(100, 50, "Empty packet", anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily);
            return emptyBuilder.Build();
        }

        var bitsPerRow = model.BitsPerRow;
        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : TitleHeight;

        // Calculate total rows needed
        var maxBit = model.Fields.Max(f => f.EndBit);
        var totalRows = (maxBit / bitsPerRow) + 1;

        var width = bitsPerRow * BitWidth + options.Padding * 2;
        var height = totalRows * RowHeight + BitNumberHeight + options.Padding * 2 + titleOffset;

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

        var baseY = options.Padding + titleOffset;

        // Draw bit numbers
        for (int i = 0; i < bitsPerRow; i++)
        {
            var x = options.Padding + i * BitWidth + BitWidth / 2;
            builder.AddText(x, baseY + BitNumberHeight / 2, i.ToString(),
                anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize - 4}px", fontFamily: options.FontFamily,
                fill: "#666");
        }

        // Draw fields
        int colorIndex = 0;
        foreach (var field in model.Fields)
        {
            var startRow = field.StartBit / bitsPerRow;
            var endRow = field.EndBit / bitsPerRow;
            var color = FieldColors[colorIndex % FieldColors.Length];

            if (startRow == endRow)
            {
                // Field fits in one row
                var startCol = field.StartBit % bitsPerRow;
                var fieldWidth = field.Width;

                var x = options.Padding + startCol * BitWidth;
                var y = baseY + BitNumberHeight + startRow * RowHeight;

                builder.AddRect(x, y, fieldWidth * BitWidth, RowHeight,
                    fill: color, stroke: "#333", strokeWidth: 1);

                builder.AddText(x + fieldWidth * BitWidth / 2, y + RowHeight / 2, field.Label,
                    anchor: "middle", baseline: "middle",
                    fontSize: $"{options.FontSize - 2}px", fontFamily: options.FontFamily,
                    fill: "#333");
            }
            else
            {
                // Field spans multiple rows
                for (int row = startRow; row <= endRow; row++)
                {
                    int colStart, colEnd;

                    if (row == startRow)
                    {
                        colStart = field.StartBit % bitsPerRow;
                        colEnd = bitsPerRow - 1;
                    }
                    else if (row == endRow)
                    {
                        colStart = 0;
                        colEnd = field.EndBit % bitsPerRow;
                    }
                    else
                    {
                        colStart = 0;
                        colEnd = bitsPerRow - 1;
                    }

                    var fieldWidth = colEnd - colStart + 1;
                    var x = options.Padding + colStart * BitWidth;
                    var y = baseY + BitNumberHeight + row * RowHeight;

                    builder.AddRect(x, y, fieldWidth * BitWidth, RowHeight,
                        fill: color, stroke: "#333", strokeWidth: 1);

                    // Only show label in first row
                    if (row == startRow)
                    {
                        builder.AddText(x + fieldWidth * BitWidth / 2, y + RowHeight / 2, field.Label,
                            anchor: "middle", baseline: "middle",
                            fontSize: $"{options.FontSize - 2}px", fontFamily: options.FontFamily,
                            fill: "#333");
                    }
                }
            }

            colorIndex++;
        }

        return builder.Build();
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
