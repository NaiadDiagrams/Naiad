using System.Globalization;
using MermaidSharp.Core;
using MermaidSharp.Core.Rendering;

namespace MermaidSharp.Diagrams.Timeline;

public class TimelineRenderer : IDiagramRenderer<TimelineModel>
{
    const double PeriodWidth = 120;
    const double PeriodMarkerRadius = 8;
    const double EventHeight = 25;
    const double EventPadding = 10;
    const double TimelineY = 80;
    const double SectionPadding = 20;
    const double TitleHeight = 40;

    static readonly string[] SectionColors =
    [
        "#E3F2FD", // light blue
        "#F3E5F5", // light purple
        "#E8F5E9", // light green
        "#FFF3E0", // light orange
        "#FCE4EC", // light pink
        "#E0F7FA"  // light cyan
    ];

    static readonly string[] PeriodColors =
    [
        "#2196F3", // blue
        "#9C27B0", // purple
        "#4CAF50", // green
        "#FF9800", // orange
        "#E91E63", // pink
        "#00BCD4"  // cyan
    ];

    public SvgDocument Render(TimelineModel model, RenderOptions options)
    {
        if (model.Sections.Count == 0 || model.Sections.All(s => s.Periods.Count == 0))
        {
            var emptyBuilder = new SvgBuilder().Size(200, 100);
            emptyBuilder.AddText(100, 50, "Empty timeline", anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily);
            return emptyBuilder.Build();
        }

        // Calculate layout
        var totalPeriods = model.Sections.Sum(s => s.Periods.Count);
        var maxEvents = model.Sections.SelectMany(s => s.Periods).Max(p => p.Events.Count);

        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : TitleHeight;
        var eventsHeight = maxEvents * EventHeight + EventPadding * 2;
        var timelineYPos = titleOffset + TimelineY + options.Padding;

        var width = totalPeriods * PeriodWidth + options.Padding * 2 + SectionPadding * model.Sections.Count;
        var height = titleOffset + TimelineY + eventsHeight + options.Padding * 2 + 40;

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

        // Draw sections and periods
        double currentX = options.Padding;
        int sectionIndex = 0;
        int globalPeriodIndex = 0;

        foreach (var section in model.Sections)
        {
            var sectionWidth = section.Periods.Count * PeriodWidth;
            var sectionColor = SectionColors[sectionIndex % SectionColors.Length];

            // Draw section background
            if (!string.IsNullOrEmpty(section.Name))
            {
                builder.AddRect(currentX, titleOffset + options.Padding, sectionWidth, height - titleOffset - options.Padding * 2,
                    fill: sectionColor, stroke: "none", rx: 5);

                // Section name
                builder.AddText(currentX + sectionWidth / 2, titleOffset + options.Padding + 15, section.Name,
                    anchor: "middle",
                    baseline: "middle",
                    fontSize: $"{options.FontSize}px",
                    fontFamily: options.FontFamily,
                    fontWeight: "bold",
                    fill: "#333");
            }

            // Draw periods in this section
            foreach (var period in section.Periods)
            {
                var periodX = currentX + (section.Periods.IndexOf(period) + 0.5) * PeriodWidth;
                var periodColor = PeriodColors[globalPeriodIndex % PeriodColors.Length];

                // Period marker
                builder.AddCircle(periodX, timelineYPos, PeriodMarkerRadius,
                    fill: periodColor, stroke: "#333", strokeWidth: 2);

                // Period label
                builder.AddText(periodX, timelineYPos - 25, period.Label,
                    anchor: "middle",
                    baseline: "middle",
                    fontSize: $"{options.FontSize}px",
                    fontFamily: options.FontFamily,
                    fontWeight: "bold",
                    fill: periodColor);

                // Draw events
                var eventY = timelineYPos + 30;
                foreach (var evt in period.Events)
                {
                    // Event box
                    var eventWidth = MeasureText(evt, options.FontSize) + EventPadding * 2;
                    var eventX = periodX - eventWidth / 2;

                    builder.AddRect(eventX, eventY, eventWidth, EventHeight - 5,
                        rx: 4,
                        fill: "#fff",
                        stroke: periodColor,
                        strokeWidth: 1);

                    builder.AddText(periodX, eventY + (EventHeight - 5) / 2, evt,
                        anchor: "middle",
                        baseline: "middle",
                        fontSize: $"{options.FontSize - 2}px",
                        fontFamily: options.FontFamily);

                    eventY += EventHeight;
                }

                globalPeriodIndex++;
            }

            currentX += sectionWidth + SectionPadding;
            sectionIndex++;
        }

        // Draw timeline line
        var lineStartX = options.Padding + PeriodWidth / 2;
        var lineEndX = currentX - SectionPadding - PeriodWidth / 2;
        builder.AddLine(lineStartX, timelineYPos, lineEndX, timelineYPos,
            stroke: "#333", strokeWidth: 3);

        return builder.Build();
    }

    static double MeasureText(string text, double fontSize)
    {
        return text.Length * fontSize * 0.55;
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
