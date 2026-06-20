namespace Naiad.Diagrams.Quadrant;

public class QuadrantRenderer : IDiagramRenderer<QuadrantModel>
{
    const double chartSize = 400;
    const double minAxisMargin = 60;
    const double titleHeight = 40;
    const double pointRadius = 8;
    const double labelPadding = 10;

    static readonly string[] quadrantColors =
    [
        "#E8F5E9", // Q1 (top-right) - green
        "#E3F2FD", // Q2 (top-left) - blue
        "#FFF3E0", // Q3 (bottom-left) - orange
        "#FCE4EC"  // Q4 (bottom-right) - pink
    ];

    static readonly string[] pointColors =
    [
        "#4CAF50",
        "#2196F3",
        "#FF9800",
        "#E91E63",
        "#9C27B0",
        "#00BCD4"
    ];

    public SvgDocument Render(QuadrantModel model, RenderOptions options)
    {
        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : titleHeight;

        // Calculate left margin based on y-axis label lengths
        var yLabelMaxLength = Math.Max(
            model.YAxisTop?.Length ?? 0,
            model.YAxisBottom?.Length ?? 0);
        var leftAxisMargin = Math.Max(minAxisMargin, yLabelMaxLength * options.FontSize * 0.6 + labelPadding);

        var width = chartSize + leftAxisMargin + minAxisMargin + options.Padding * 2;
        var height = chartSize + minAxisMargin * 2 + titleOffset + options.Padding * 2;

        var builder = new SvgBuilder().Size(width, height);

        var chartLeft = options.Padding + leftAxisMargin;
        var chartTop = options.Padding + titleOffset + minAxisMargin;
        var chartRight = chartLeft + chartSize;
        var chartBottom = chartTop + chartSize;
        var centerX = chartLeft + chartSize / 2;
        var centerY = chartTop + chartSize / 2;

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

        // Draw quadrant backgrounds
        const double halfSize = chartSize / 2;

        // Q2 (top-left)
        builder.AddRect(
            chartLeft,
            chartTop,
            halfSize,
            halfSize,
            fill: quadrantColors[1],
            stroke: "#ccc",
            strokeWidth: 1);
        // Q1 (top-right)
        builder.AddRect(
            centerX,
            chartTop,
            halfSize,
            halfSize,
            fill: quadrantColors[0],
            stroke: "#ccc",
            strokeWidth: 1);
        // Q3 (bottom-left)
        builder.AddRect(
            chartLeft,
            centerY,
            halfSize,
            halfSize,
            fill: quadrantColors[2],
            stroke: "#ccc",
            strokeWidth: 1);
        // Q4 (bottom-right)
        builder.AddRect(
            centerX,
            centerY,
            halfSize,
            halfSize,
            fill: quadrantColors[3],
            stroke: "#ccc",
            strokeWidth: 1);

        // Draw quadrant labels
        if (!string.IsNullOrEmpty(model.Quadrant1Label))
        {
            builder.AddText(
                centerX + halfSize / 2,
                chartTop + 20,
                model.Quadrant1Label,
                anchor: "middle",
                baseline: "middle",
                fontSize: options.FontSize - 2,
                fontFamily: options.FontFamily,
                fill: "#666");
        }
        if (!string.IsNullOrEmpty(model.Quadrant2Label))
        {
            builder.AddText(
                chartLeft + halfSize / 2,
                chartTop + 20,
                model.Quadrant2Label,
                anchor: "middle",
                baseline: "middle",
                fontSize: options.FontSize - 2,
                fontFamily: options.FontFamily,
                fill: "#666");
        }
        if (!string.IsNullOrEmpty(model.Quadrant3Label))
        {
            builder.AddText(
                chartLeft + halfSize / 2,
                centerY + 20,
                model.Quadrant3Label,
                anchor: "middle",
                baseline: "middle",
                fontSize: options.FontSize - 2,
                fontFamily: options.FontFamily,
                fill: "#666");
        }
        if (!string.IsNullOrEmpty(model.Quadrant4Label))
        {
            builder.AddText(
                centerX + halfSize / 2,
                centerY + 20,
                model.Quadrant4Label,
                anchor: "middle",
                baseline: "middle",
                fontSize: options.FontSize - 2,
                fontFamily: options.FontFamily,
                fill: "#666");
        }

        // Draw axes
        builder.AddLine(
            chartLeft,
            centerY,
            chartRight,
            centerY,
            stroke: "#333",
            strokeWidth: 2);
        builder.AddLine(
            centerX,
            chartTop,
            centerX,
            chartBottom,
            stroke: "#333",
            strokeWidth: 2);

        // Draw axis labels
        if (!string.IsNullOrEmpty(model.XAxisLeft))
        {
            builder.AddText(
                chartLeft,
                chartBottom + 30,
                model.XAxisLeft,
                anchor: "start",
                baseline: "middle",
                fontSize: options.FontSize,
                fontFamily: options.FontFamily,
                fill: "#333");
        }
        if (!string.IsNullOrEmpty(model.XAxisRight))
        {
            builder.AddText(
                chartRight,
                chartBottom + 30,
                model.XAxisRight,
                anchor: "end",
                baseline: "middle",
                fontSize: options.FontSize,
                fontFamily: options.FontFamily,
                fill: "#333");
        }
        if (!string.IsNullOrEmpty(model.YAxisBottom))
        {
            builder.AddText(
                chartLeft - 10,
                chartBottom,
                model.YAxisBottom,
                anchor: "end",
                baseline: "middle",
                fontSize: options.FontSize,
                fontFamily: options.FontFamily,
                fill: "#333");
        }
        if (!string.IsNullOrEmpty(model.YAxisTop))
        {
            builder.AddText(
                chartLeft - 10,
                chartTop,
                model.YAxisTop,
                anchor: "end",
                baseline: "middle",
                fontSize: options.FontSize,
                fontFamily: options.FontFamily,
                fill: "#333");
        }

        // Draw points
        for (var i = 0; i < model.Points.Count; i++)
        {
            var point = model.Points[i];
            var pointColor = pointColors[i % pointColors.Length];

            var px = chartLeft + point.X * chartSize;
            var py = chartBottom - point.Y * chartSize; // Y is inverted (0 at bottom)

            // Point circle
            builder.AddCircle(
                px,
                py,
                pointRadius,
                fill: pointColor,
                stroke: "#333",
                strokeWidth: 2);

            // Point label
            builder.AddText(
                px + pointRadius + 5,
                py,
                point.Name,
                anchor: "start",
                baseline: "middle",
                fontSize: options.FontSize - 2,
                fontFamily: options.FontFamily,
                fill: "#333");
        }

        return builder.Build();
    }
}
