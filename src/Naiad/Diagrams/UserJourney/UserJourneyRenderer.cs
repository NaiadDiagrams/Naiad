namespace MermaidSharp.Diagrams.UserJourney;

public class UserJourneyRenderer : IDiagramRenderer<UserJourneyModel>
{
    const double TaskWidth = 150;
    const double TaskHeight = 60;
    const double TaskMargin = 20;
    const double SectionPadding = 15;
    const double TitleHeight = 40;
    const double ActorRowHeight = 30;

    // Score colors from red (1) to green (5)
    static readonly string[] ScoreColors =
    [
        "#FF6B6B", // 1 - red
        "#FFA94D", // 2 - orange
        "#FFE066", // 3 - yellow
        "#8CE99A", // 4 - light green
        "#51CF66"  // 5 - green
    ];

    static readonly string[] SectionColors =
    [
        "#E3F2FD",
        "#F3E5F5",
        "#E8F5E9",
        "#FFF3E0",
        "#FCE4EC"
    ];

    public SvgDocument Render(UserJourneyModel model, RenderOptions options)
    {
        if (model.Sections.Count == 0 || model.Sections.All(s => s.Tasks.Count == 0))
        {
            var emptyBuilder = new SvgBuilder().Size(200, 100);
            emptyBuilder.AddText(100, 50, "Empty journey", anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily);
            return emptyBuilder.Build();
        }

        // Collect all unique actors
        var allActors = model.Sections
            .SelectMany(s => s.Tasks)
            .SelectMany(t => t.Actors)
            .Distinct()
            .ToList();

        // Calculate layout
        var maxTasks = model.Sections.Max(s => s.Tasks.Count);
        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : TitleHeight;
        var actorsHeight = allActors.Count * ActorRowHeight + SectionPadding;

        var width = maxTasks * (TaskWidth + TaskMargin) + options.Padding * 2 + TaskMargin;
        var sectionHeight = TaskHeight + SectionPadding * 2;
        var height = titleOffset + model.Sections.Count * sectionHeight + actorsHeight + options.Padding * 2;

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

        // Draw actors legend on the right
        var legendX = width - options.Padding - 100;
        var legendY = titleOffset + options.Padding + 10;

        builder.AddText(legendX, legendY, "Actors:",
            anchor: "start",
            baseline: "middle",
            fontSize: $"{options.FontSize}px",
            fontFamily: options.FontFamily,
            fontWeight: "bold");

        for (var i = 0; i < allActors.Count; i++)
        {
            var actorY = legendY + (i + 1) * ActorRowHeight;
            var actorColor = GetActorColor(i);

            builder.AddCircle(legendX + 10, actorY, 8, fill: actorColor, stroke: "#333", strokeWidth: 1);
            builder.AddText(legendX + 25, actorY, allActors[i],
                anchor: "start",
                baseline: "middle",
                fontSize: $"{options.FontSize - 2}px",
                fontFamily: options.FontFamily);
        }

        // Draw sections
        var currentY = titleOffset + options.Padding;
        var sectionIndex = 0;

        foreach (var section in model.Sections)
        {
            var sectionColor = SectionColors[sectionIndex % SectionColors.Length];

            // Section background
            builder.AddRect(options.Padding, currentY, width - options.Padding * 2 - 120, sectionHeight,
                fill: sectionColor, stroke: "none", rx: 5);

            // Section name
            if (!string.IsNullOrEmpty(section.Name))
            {
                builder.AddText(options.Padding + 10, currentY + 15, section.Name,
                    anchor: "start",
                    baseline: "middle",
                    fontSize: $"{options.FontSize}px",
                    fontFamily: options.FontFamily,
                    fontWeight: "bold",
                    fill: "#333");
            }

            // Draw tasks
            var taskX = options.Padding + TaskMargin;
            var taskY = currentY + SectionPadding + 15;

            foreach (var task in section.Tasks)
            {
                var scoreColor = ScoreColors[Math.Clamp(task.Score - 1, 0, 4)];

                // Task card
                builder.AddRect(taskX, taskY, TaskWidth, TaskHeight,
                    fill: "#fff",
                    stroke: scoreColor,
                    strokeWidth: 2,
                    rx: 8);

                // Score indicator bar at top
                builder.AddRect(taskX, taskY, TaskWidth, 8,
                    fill: scoreColor,
                    stroke: "none",
                    rx: 8);
                // Cover bottom corners of score bar
                builder.AddRect(taskX, taskY + 4, TaskWidth, 4,
                    fill: scoreColor,
                    stroke: "none");

                // Task name
                builder.AddText(taskX + TaskWidth / 2, taskY + 25, task.Name,
                    anchor: "middle",
                    baseline: "middle",
                    fontSize: $"{options.FontSize - 1}px",
                    fontFamily: options.FontFamily);

                // Score badge
                builder.AddText(taskX + TaskWidth / 2, taskY + 45, $"Score: {task.Score}",
                    anchor: "middle",
                    baseline: "middle",
                    fontSize: $"{options.FontSize - 2}px",
                    fontFamily: options.FontFamily,
                    fill: "#666");

                // Actor indicators
                var actorX = taskX + 5;
                foreach (var actor in task.Actors)
                {
                    var actorIndex = allActors.IndexOf(actor);
                    var actorColor = GetActorColor(actorIndex);
                    builder.AddCircle(actorX + 5, taskY + TaskHeight - 8, 5,
                        fill: actorColor, stroke: "#333", strokeWidth: 1);
                    actorX += 15;
                }

                taskX += TaskWidth + TaskMargin;
            }

            currentY += sectionHeight;
            sectionIndex++;
        }

        return builder.Build();
    }

    static string GetActorColor(int index)
    {
        string[] colors = ["#4ECDC4", "#45B7D1", "#96CEB4", "#FFEAA7", "#DDA0DD", "#98D8C8"];
        return colors[index % colors.Length];
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
