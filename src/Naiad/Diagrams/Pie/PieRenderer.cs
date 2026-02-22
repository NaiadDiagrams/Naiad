namespace MermaidSharp.Diagrams.Pie;

public class PieRenderer : IDiagramRenderer<PieModel>
{
    // Mermaid.ink default pie colors
    static readonly string[] DefaultColors =
    [
        "#ECECFF",                          // lavender
        "#ffffde",                          // light yellow
        "hsl(80, 100%, 56.2745098039%)",    // lime green
        "hsl(240, 100%, 86.2745098039%)",   // light blue
        "hsl(0, 100%, 86.2745098039%)",     // light red
        "hsl(160, 100%, 56.2745098039%)",   // teal
        "hsl(40, 100%, 56.2745098039%)",    // orange
        "hsl(280, 100%, 76.2745098039%)"    // purple
    ];

    // RGB equivalents for legend (mermaid converts HSL to RGB for style attribute)
    static readonly string[] DefaultColorRgb =
    [
        "rgb(236, 236, 255)",               // #ECECFF
        "rgb(255, 255, 222)",               // #ffffde
        "rgb(181, 255, 32)",                // hsl(80, 100%, 56.27%)
        "rgb(185, 185, 255)",               // hsl(240, 100%, 86.27%)
        "rgb(255, 185, 185)",               // hsl(0, 100%, 86.27%)
        "rgb(32, 255, 181)",                // hsl(160, 100%, 56.27%)
        "rgb(255, 181, 32)",                // hsl(40, 100%, 56.27%)
        "rgb(215, 134, 255)"                // hsl(280, 100%, 76.27%)
    ];

    const double Radius = 185.0;
    const double OuterRadius = 186.0;

    public SvgDocument Render(PieModel model, RenderOptions options)
    {
        var total = model.Sections.Sum(_ => _.Value);
        if (total == 0) total = 1;

        // Calculate legend labels (with values if showData is enabled)
        var legendLabels = model.Sections.Select(_ =>
            model.ShowData ? $"{_.Label} [{(int)_.Value}]" : _.Label).ToList();

        // Match mermaid.ink exact dimensions - width varies based on legend text
        var width = model.ShowData ? 613.140625 : 551.6875;
        var height = 450.0;
        var cx = 225.0;
        var cy = 225.0;

        var builder = new SvgBuilder()
            .Size(width, height)
            .DiagramType(null!, "pie")
            .AddStyles(MermaidStyles.PieStyles);

        // Empty group (mermaid.ink artifact)
        builder.BeginGroup();
        builder.EndGroup();

        // Main pie group with transform to center
        builder.BeginGroup(transform: $"translate({Fmt(cx)},{Fmt(cy)})");

        // Outer circle
        builder.AddCircle(0, 0, OuterRadius, cssClass: "pieOuterCircle");

        // Draw pie slices
        double startAngle = 0;
        for (var i = 0; i < model.Sections.Count; i++)
        {
            var section = model.Sections[i];
            var sweepAngle = section.Value / total * 360;
            var color = section.Color ?? DefaultColors[i % DefaultColors.Length];

            var path = CreateMermaidArcPath(startAngle, sweepAngle);
            builder.AddPath(path, fill: color, cssClass: "pieCircle");

            startAngle += sweepAngle;
        }

        // Add percentage labels with mermaid.ink exact positions
        startAngle = 0;
        for (var i = 0; i < model.Sections.Count; i++)
        {
            var section = model.Sections[i];
            var sweepAngle = section.Value / total * 360;
            var percentage = (int)Math.Round(section.Value / total * 100);

            // Mermaid uses a label radius factor of 0.75 (138.75 / 185)
            var midAngle = startAngle + sweepAngle / 2;
            var labelDist = Radius * 0.75; // Exact mermaid factor
            var labelX = labelDist * Math.Sin(ToRadians(midAngle));
            var labelY = -labelDist * Math.Cos(ToRadians(midAngle));

            builder.AddText(0, 0, $"{percentage}%",
                cssClass: "slice",
                style: "text-anchor: middle;",
                transform: $"translate({FmtMermaid(labelX)},{FmtMermaid(labelY)})",
                omitXY: true);

            startAngle += sweepAngle;
        }

        // Title (empty element if no title - self-closing)
        builder.AddText(0, -200, model.Title ?? "", cssClass: "pieTitleText");

        // Legend items
        var legendStartY = -(model.Sections.Count * 22) / 2;
        for (var i = 0; i < model.Sections.Count; i++)
        {
            var section = model.Sections[i];
            var colorRgb = GetRgbColor(section.Color, i);
            var itemY = legendStartY + i * 22;

            builder.BeginGroup(cssClass: "legend", transform: $"translate(216,{itemY})");
            builder.AddRectNoXY(18, 18, style: $"fill: {colorRgb}; stroke: {colorRgb};");
            builder.AddText(22, 14, legendLabels[i]);
            builder.EndGroup();
        }

        builder.EndGroup();

        return builder.Build();
    }

    static string CreateMermaidArcPath(double startAngle, double sweepAngle)
    {
        // Mermaid uses: M startX,startY A r,r 0 largeArc,1 endX,endY L 0,0 Z
        // Angles start from top (12 o'clock) and go clockwise
        var startRad = ToRadians(startAngle);
        var endRad = ToRadians(startAngle + sweepAngle);
        var largeArc = sweepAngle > 180 ? 1 : 0;

        var x1 = Radius * Math.Sin(startRad);
        var y1 = -Radius * Math.Cos(startRad);
        var x2 = Radius * Math.Sin(endRad);
        var y2 = -Radius * Math.Cos(endRad);

        // Fix -0 to 0 (happens at 360 degree angles due to floating point)
        if (Math.Abs(x2) < 1e-10) x2 = 0;
        if (Math.Abs(y2) < 1e-10) y2 = 0;

        // Match mermaid's precision (2-3 decimal places)
        return $"M{FmtPath(x1)},{FmtPath(y1)}A{Radius},{Radius},0,{largeArc},1,{FmtPath(x2)},{FmtPath(y2)}L0,0Z";
    }

    static string GetRgbColor(string? color, int index)
    {
        if (color is not null)
        {
            return color.StartsWith("rgb") ? color : ConvertToRgb(color);
        }
        return DefaultColorRgb[index % DefaultColorRgb.Length];
    }

    static string ConvertToRgb(string color)
    {
        if (color.StartsWith('#') && color.Length == 7)
        {
            var r = Convert.ToInt32(color.Substring(1, 2), 16);
            var g = Convert.ToInt32(color.Substring(3, 2), 16);
            var b = Convert.ToInt32(color.Substring(5, 2), 16);
            return $"rgb({r}, {g}, {b})";
        }
        return color;
    }

    static double ToRadians(double degrees) => degrees * Math.PI / 180;
    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
    static string FmtPath(double value) => Math.Round(value, 3).ToString("0.###", CultureInfo.InvariantCulture);
    // Use R format to get full precision, then remove unnecessary trailing zeros
    static string FmtMermaid(double value)
    {
        var s = value.ToString("R", CultureInfo.InvariantCulture);
        // R format may produce exponential notation which we don't want
        if (s.Contains('E') || s.Contains('e'))
        {
            s = value.ToString("0.#################", CultureInfo.InvariantCulture);
        }
        return s;
    }
}
