namespace Naiad.Diagrams.Packet;

public class PacketRenderer : IDiagramRenderer<PacketModel>
{
    const double bitWidth = 20;
    const double rowHeight = 40;
    const double bitNumberHeight = 20;
    const double titleHeight = 40;

    static readonly string[] fieldColors =
    [
        "#E3F2FD", "#E8F5E9", "#FFF3E0", "#F3E5F5",
        "#FCE4EC", "#E0F7FA", "#FFF8E1", "#F1F8E9"
    ];

    public SvgDocument Render(PacketModel model, RenderOptions options)
    {
        if (model.Fields.Count == 0)
        {
            var emptyBuilder = new SvgBuilder().Size(200, 100);
            emptyBuilder.AddText(
                100,
                50,
                "Empty packet",
                anchor: "middle",
                baseline: "middle",
                fontSize: options.FontSize,
                fontFamily: options.FontFamily);
            return emptyBuilder.Build();
        }

        var bitsPerRow = model.BitsPerRow;
        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : titleHeight;

        // Calculate total rows needed
        var maxBit = model.Fields.Max(f => f.EndBit);
        var totalRows = maxBit / bitsPerRow + 1;

        var width = bitsPerRow * bitWidth + options.Padding * 2;
        var height = totalRows * rowHeight + bitNumberHeight + options.Padding * 2 + titleOffset;

        var builder = new SvgBuilder().Size(width, height);

        // Draw title
        if (!string.IsNullOrEmpty(model.Title))
        {
            builder.AddText(
                width / 2,
                options.Padding + titleHeight / 2,
                model.Title,
                anchor: "middle",
                baseline: "middle",
                fontSize: options.FontSize + 4,
                fontFamily: options.FontFamily,
                fontWeight: "bold");
        }

        var baseY = options.Padding + titleOffset;

        // Draw bit numbers
        for (var i = 0; i < bitsPerRow; i++)
        {
            var x = options.Padding + i * bitWidth + bitWidth / 2;
            builder.AddText(
                x,
                baseY + bitNumberHeight / 2,
                i.ToString(),
                anchor: "middle",
                baseline: "middle",
                fontSize: options.FontSize - 4,
                fontFamily: options.FontFamily,
                fill: "#666");
        }

        // Draw fields
        var colorIndex = 0;
        foreach (var field in model.Fields)
        {
            var startRow = field.StartBit / bitsPerRow;
            var endRow = field.EndBit / bitsPerRow;
            var color = fieldColors[colorIndex % fieldColors.Length];

            if (startRow == endRow)
            {
                // Field fits in one row
                var startCol = field.StartBit % bitsPerRow;
                var fieldWidth = field.Width;

                var x = options.Padding + startCol * bitWidth;
                var y = baseY + bitNumberHeight + startRow * rowHeight;

                builder.AddRect(
                    x,
                    y,
                    fieldWidth * bitWidth,
                    rowHeight,
                    fill: color,
                    stroke: "#333",
                    strokeWidth: 1);

                builder.AddText(
                    x + fieldWidth * bitWidth / 2,
                    y + rowHeight / 2,
                    field.Label,
                    anchor: "middle",
                    baseline: "middle",
                    fontSize: options.FontSize - 2,
                    fontFamily: options.FontFamily,
                    fill: "#333");
            }
            else
            {
                // Field spans multiple rows
                for (var row = startRow; row <= endRow; row++)
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
                    var x = options.Padding + colStart * bitWidth;
                    var y = baseY + bitNumberHeight + row * rowHeight;

                    builder.AddRect(
                        x,
                        y,
                        fieldWidth * bitWidth,
                        rowHeight,
                        fill: color,
                        stroke: "#333",
                        strokeWidth: 1);

                    // Only show label in first row
                    if (row == startRow)
                    {
                        builder.AddText(
                            x + fieldWidth * bitWidth / 2,
                            y + rowHeight / 2,
                            field.Label,
                            anchor: "middle",
                            baseline: "middle",
                            fontSize: options.FontSize - 2,
                            fontFamily: options.FontFamily,
                            fill: "#333");
                    }
                }
            }

            colorIndex++;
        }

        return builder.Build();
    }
}
