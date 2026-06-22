namespace Naiad.Diagrams.Timeline;

public class TimelineRenderer : IDiagramRenderer<TimelineModel>
{
    const double periodWidth = 120;
    const double periodMarkerRadius = 8;
    const double eventHeight = 25;
    const double eventPadding = 10;
    const double timelineY = 80;
    const double sectionPadding = 20;
    const double titleHeight = 40;

    static string[] sectionColors =
    [
        "#E3F2FD", // light blue
        "#F3E5F5", // light purple
        "#E8F5E9", // light green
        "#FFF3E0", // light orange
        "#FCE4EC", // light pink
        "#E0F7FA"  // light cyan
    ];

    static string[] periodColors =
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
        if (model.Sections.Count == 0 ||
            model.Sections.All(_ => _.Periods.Count == 0))
        {
            var emptyBuilder = new SvgBuilder().Size(200, 100);
            emptyBuilder.AddText(
                100,
                50,
                "Empty timeline",
                anchor: "middle",
                baseline: "middle",
                fontSize: options.FontSize,
                fontFamily: options.FontFamily);
            return emptyBuilder.Build();
        }

        // Calculate layout
        var totalPeriods = model.Sections.Sum(_ => _.Periods.Count);
        var maxEvents = model.Sections.SelectMany(_ => _.Periods).Max(_ => _.Events.Count);

        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : titleHeight;
        var eventsHeight = maxEvents * eventHeight + eventPadding * 2;
        var timelineYPos = titleOffset + timelineY + options.Padding;

        var width = totalPeriods * periodWidth + options.Padding * 2 + sectionPadding * model.Sections.Count;
        var height = titleOffset + timelineY + eventsHeight + options.Padding * 2 + 40;

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

        // Draw sections and periods
        var currentX = options.Padding;
        var sectionIndex = 0;
        var globalPeriodIndex = 0;

        foreach (var section in model.Sections)
        {
            var sectionWidth = section.Periods.Count * periodWidth;
            var sectionColor = sectionColors[sectionIndex % sectionColors.Length];

            // Draw section background
            if (!string.IsNullOrEmpty(section.Name))
            {
                builder.AddRect(
                    currentX,
                    titleOffset + options.Padding,
                    sectionWidth,
                    height - titleOffset - options.Padding * 2,
                    fill: sectionColor,
                    stroke: "none",
                    rx: 5);

                // Section name
                builder.AddText(
                    currentX + sectionWidth / 2,
                    titleOffset + options.Padding + 15,
                    section.Name,
                    anchor: "middle",
                    baseline: "middle",
                    fontSize: options.FontSize,
                    fontFamily: options.FontFamily,
                    fontWeight: "bold",
                    fill: "#333");
            }

            // Draw periods in this section
            foreach (var period in section.Periods)
            {
                var periodX = currentX + (section.Periods.IndexOf(period) + 0.5) * periodWidth;
                var periodColor = periodColors[globalPeriodIndex % periodColors.Length];

                // Period marker
                builder.AddCircle(
                    periodX,
                    timelineYPos,
                    periodMarkerRadius,
                    fill: periodColor,
                    stroke: "#333",
                    strokeWidth: 2);

                // Period label
                builder.AddText(
                    periodX,
                    timelineYPos - 25,
                    period.Label,
                    anchor: "middle",
                    baseline: "middle",
                    fontSize: options.FontSize,
                    fontFamily: options.FontFamily,
                    fontWeight: "bold",
                    fill: periodColor);

                // Draw events
                var eventY = timelineYPos + 30;
                foreach (var evt in period.Events)
                {
                    // Event box
                    var eventWidth = MeasureText(evt, options.FontSize) + eventPadding * 2;
                    var eventX = periodX - eventWidth / 2;

                    builder.AddRect(
                        eventX,
                        eventY,
                        eventWidth,
                        eventHeight - 5,
                        rx: 4,
                        fill: "#fff",
                        stroke: periodColor,
                        strokeWidth: 1);

                    builder.AddText(
                        periodX,
                        eventY + (eventHeight - 5) / 2,
                        evt,
                        anchor: "middle",
                        baseline: "middle",
                        fontSize: options.FontSize - 2,
                        fontFamily: options.FontFamily);

                    eventY += eventHeight;
                }

                globalPeriodIndex++;
            }

            currentX += sectionWidth + sectionPadding;
            sectionIndex++;
        }

        // Draw timeline line
        var lineStartX = options.Padding + periodWidth / 2;
        var lineEndX = currentX - sectionPadding - periodWidth / 2;
        builder.AddLine(
            lineStartX,
            timelineYPos,
            lineEndX,
            timelineYPos,
            stroke: "#333",
            strokeWidth: 3);

        return builder.Build();
    }

    static double MeasureText(string text, double fontSize) =>
        text.Length * fontSize * 0.55;
}
