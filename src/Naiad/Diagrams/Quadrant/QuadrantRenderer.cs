namespace MermaidSharp.Diagrams.Quadrant;

public class QuadrantRenderer : IDiagramRenderer<QuadrantModel>
{
    const double ChartSize = 400;
    const double AxisMargin = 60;
    const double TitleHeight = 40;
    const double PointRadius = 8;
    const double LabelPadding = 10;

    static readonly string[] QuadrantColors =
    [
        "#E8F5E9", // Q1 (top-right) - green
        "#E3F2FD", // Q2 (top-left) - blue
        "#FFF3E0", // Q3 (bottom-left) - orange
        "#FCE4EC"  // Q4 (bottom-right) - pink
    ];

    static readonly string[] PointColors =
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
        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : TitleHeight;
        var width = ChartSize + AxisMargin * 2 + options.Padding * 2;
        var height = ChartSize + AxisMargin * 2 + titleOffset + options.Padding * 2;

        var builder = new SvgBuilder().Size(width, height);

        var chartLeft = options.Padding + AxisMargin;
        var chartTop = options.Padding + titleOffset + AxisMargin;
        var chartRight = chartLeft + ChartSize;
        var chartBottom = chartTop + ChartSize;
        var centerX = chartLeft + ChartSize / 2;
        var centerY = chartTop + ChartSize / 2;

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

        // Draw quadrant backgrounds
        var halfSize = ChartSize / 2;

        // Q2 (top-left)
        builder.AddRect(chartLeft, chartTop, halfSize, halfSize,
            fill: QuadrantColors[1], stroke: "#ccc", strokeWidth: 1);
        // Q1 (top-right)
        builder.AddRect(centerX, chartTop, halfSize, halfSize,
            fill: QuadrantColors[0], stroke: "#ccc", strokeWidth: 1);
        // Q3 (bottom-left)
        builder.AddRect(chartLeft, centerY, halfSize, halfSize,
            fill: QuadrantColors[2], stroke: "#ccc", strokeWidth: 1);
        // Q4 (bottom-right)
        builder.AddRect(centerX, centerY, halfSize, halfSize,
            fill: QuadrantColors[3], stroke: "#ccc", strokeWidth: 1);

        // Draw quadrant labels
        if (!string.IsNullOrEmpty(model.Quadrant1Label))
        {
            builder.AddText(centerX + halfSize / 2, chartTop + 20, model.Quadrant1Label,
                anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize - 2}px", fontFamily: options.FontFamily,
                fill: "#666");
        }
        if (!string.IsNullOrEmpty(model.Quadrant2Label))
        {
            builder.AddText(chartLeft + halfSize / 2, chartTop + 20, model.Quadrant2Label,
                anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize - 2}px", fontFamily: options.FontFamily,
                fill: "#666");
        }
        if (!string.IsNullOrEmpty(model.Quadrant3Label))
        {
            builder.AddText(chartLeft + halfSize / 2, centerY + 20, model.Quadrant3Label,
                anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize - 2}px", fontFamily: options.FontFamily,
                fill: "#666");
        }
        if (!string.IsNullOrEmpty(model.Quadrant4Label))
        {
            builder.AddText(centerX + halfSize / 2, centerY + 20, model.Quadrant4Label,
                anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize - 2}px", fontFamily: options.FontFamily,
                fill: "#666");
        }

        // Draw axes
        builder.AddLine(chartLeft, centerY, chartRight, centerY,
            stroke: "#333", strokeWidth: 2);
        builder.AddLine(centerX, chartTop, centerX, chartBottom,
            stroke: "#333", strokeWidth: 2);

        // Draw axis labels
        if (!string.IsNullOrEmpty(model.XAxisLeft))
        {
            builder.AddText(chartLeft, chartBottom + 30, model.XAxisLeft,
                anchor: "start", baseline: "middle",
                fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily,
                fill: "#333");
        }
        if (!string.IsNullOrEmpty(model.XAxisRight))
        {
            builder.AddText(chartRight, chartBottom + 30, model.XAxisRight,
                anchor: "end", baseline: "middle",
                fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily,
                fill: "#333");
        }
        if (!string.IsNullOrEmpty(model.YAxisBottom))
        {
            builder.AddText(chartLeft - 10, chartBottom, model.YAxisBottom,
                anchor: "end", baseline: "middle",
                fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily,
                fill: "#333");
        }
        if (!string.IsNullOrEmpty(model.YAxisTop))
        {
            builder.AddText(chartLeft - 10, chartTop, model.YAxisTop,
                anchor: "end", baseline: "middle",
                fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily,
                fill: "#333");
        }

        // Draw points
        for (var i = 0; i < model.Points.Count; i++)
        {
            var point = model.Points[i];
            var pointColor = PointColors[i % PointColors.Length];

            var px = chartLeft + point.X * ChartSize;
            var py = chartBottom - point.Y * ChartSize; // Y is inverted (0 at bottom)

            // Point circle
            builder.AddCircle(px, py, PointRadius,
                fill: pointColor, stroke: "#333", strokeWidth: 2);

            // Point label
            builder.AddText(px + PointRadius + 5, py, point.Name,
                anchor: "start", baseline: "middle",
                fontSize: $"{options.FontSize - 2}px", fontFamily: options.FontFamily,
                fill: "#333");
        }

        return builder.Build();
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
