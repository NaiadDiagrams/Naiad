using System.Globalization;
using MermaidSharp.Core;
using MermaidSharp.Core.Models;
using MermaidSharp.Core.Rendering;

namespace MermaidSharp.Diagrams.Pie;

public class PieRenderer : IDiagramRenderer<PieModel>
{
    static readonly string[] DefaultColors =
    [
        "#4e79a7", "#f28e2c", "#e15759", "#76b7b2",
        "#59a14f", "#edc949", "#af7aa1", "#ff9da7",
        "#9c755f", "#bab0ab"
    ];

    public SvgDocument Render(PieModel model, RenderOptions options)
    {
        var total = model.Sections.Sum(s => s.Value);
        if (total == 0) total = 1;

        var size = 400.0;
        var cx = size / 2;
        var cy = size / 2;
        var radius = (size - options.Padding * 2) / 2 - 40;
        var innerRadius = model.ShowData ? radius * 0.5 : 0;

        var builder = new SvgBuilder().Size(size, size);

        // Add title if present
        var titleHeight = 0.0;
        if (!string.IsNullOrEmpty(model.Title))
        {
            titleHeight = 30;
            builder.AddText(cx, 20, model.Title,
                anchor: "middle",
                fontSize: "16px",
                fontFamily: options.FontFamily,
                fontWeight: "bold");
        }

        var chartCy = cy + titleHeight / 2;

        // Draw pie sections
        double startAngle = 0;
        for (int i = 0; i < model.Sections.Count; i++)
        {
            var section = model.Sections[i];
            var sweepAngle = (section.Value / total) * 360;
            var color = section.Color ?? DefaultColors[i % DefaultColors.Length];

            var path = CreateArcPath(cx, chartCy, radius, innerRadius, startAngle, sweepAngle);
            builder.AddPath(path, fill: color, stroke: "#fff", strokeWidth: 2);

            // Add label
            var labelAngle = startAngle + sweepAngle / 2;
            var labelRadius = radius + 20;
            var labelX = cx + labelRadius * Math.Cos(ToRadians(labelAngle - 90));
            var labelY = chartCy + labelRadius * Math.Sin(ToRadians(labelAngle - 90));

            var anchor = labelAngle > 180 ? "end" : "start";
            if (Math.Abs(labelAngle - 90) < 10 || Math.Abs(labelAngle - 270) < 10)
                anchor = "middle";

            var labelText = model.ShowData
                ? $"{section.Label}: {section.Value:0.#} ({section.Value / total * 100:0.#}%)"
                : section.Label;

            builder.AddText(labelX, labelY, labelText,
                anchor: anchor,
                baseline: "middle",
                fontSize: $"{options.FontSize}px",
                fontFamily: options.FontFamily);

            startAngle += sweepAngle;
        }

        return builder.Build();
    }

    static string CreateArcPath(double cx, double cy, double outerRadius, double innerRadius,
        double startAngle, double sweepAngle)
    {
        var startRad = ToRadians(startAngle - 90);
        var endRad = ToRadians(startAngle + sweepAngle - 90);
        var largeArc = sweepAngle > 180 ? 1 : 0;

        var x1 = cx + outerRadius * Math.Cos(startRad);
        var y1 = cy + outerRadius * Math.Sin(startRad);
        var x2 = cx + outerRadius * Math.Cos(endRad);
        var y2 = cy + outerRadius * Math.Sin(endRad);

        if (innerRadius > 0)
        {
            var ix1 = cx + innerRadius * Math.Cos(startRad);
            var iy1 = cy + innerRadius * Math.Sin(startRad);
            var ix2 = cx + innerRadius * Math.Cos(endRad);
            var iy2 = cy + innerRadius * Math.Sin(endRad);

            return $"M{Fmt(x1)},{Fmt(y1)} " +
                   $"A{Fmt(outerRadius)},{Fmt(outerRadius)} 0 {largeArc} 1 {Fmt(x2)},{Fmt(y2)} " +
                   $"L{Fmt(ix2)},{Fmt(iy2)} " +
                   $"A{Fmt(innerRadius)},{Fmt(innerRadius)} 0 {largeArc} 0 {Fmt(ix1)},{Fmt(iy1)} Z";
        }

        return $"M{Fmt(cx)},{Fmt(cy)} " +
               $"L{Fmt(x1)},{Fmt(y1)} " +
               $"A{Fmt(outerRadius)},{Fmt(outerRadius)} 0 {largeArc} 1 {Fmt(x2)},{Fmt(y2)} Z";
    }

    static double ToRadians(double degrees) => degrees * Math.PI / 180;
    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
