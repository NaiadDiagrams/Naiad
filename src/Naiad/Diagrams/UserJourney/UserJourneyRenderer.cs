namespace Naiad.Diagrams.UserJourney;

public class UserJourneyRenderer : IDiagramRenderer<UserJourneyModel>
{
    const double LeftMargin = 150;
    const double TaskWidth = 150;
    const double TaskGap = 50;
    const double TaskHeight = 55;
    const double CornerRadius = 5;
    const double SectionBarHeight = 38;
    const double SectionTaskGap = 8;
    const double TitleHeight = 40;
    const double AxisGap = 35;
    const double FaceGap = 60;
    const double FaceStep = 38;
    const double FaceRadius = 15;
    const double RightMargin = 40;

    // Per-section pastel fills for the header bars.
    static readonly string[] SectionFills =
    [
        "#DCE3F5",
        "#FBF6C6",
        "#FBE2EC",
        "#DDEFD2",
        "#FCE5CB",
        "#E7DCF4",
        "#D3EFEF"
    ];

    // Softer section-tinted borders for the white task cards.
    static readonly string[] SectionBorders =
    [
        "#9AA7D6",
        "#CFC65E",
        "#DD9CBB",
        "#93C277",
        "#E0AE73",
        "#B097D2",
        "#79C7C7"
    ];

    static readonly string[] ActorColors =
    [
        "#66BB6A",
        "#42A5F5",
        "#FFCA28",
        "#EF5350",
        "#AB47BC",
        "#26C6DA",
        "#FFA726",
        "#26A69A"
    ];

    const string TaskFill = "#FFFFFF";
    const string FaceFill = "#F0EAD0";
    const string FaceStroke = "#8C8455";
    const string FaceFeature = "#3F3F3F";

    public SvgDocument Render(UserJourneyModel model, RenderOptions options)
    {
        var sections = model.Sections.Where(_ => _.Tasks.Count > 0).ToList();
        if (sections.Count == 0)
        {
            var emptyBuilder = new SvgBuilder().Size(200, 100);
            emptyBuilder.AddText(
                100,
                50,
                "Empty journey",
                anchor: "middle",
                baseline: "middle",
                fontSize: options.FontSize,
                fontFamily: options.FontFamily);
            return emptyBuilder.Build();
        }

        var tasks = sections.SelectMany(_ => _.Tasks).ToList();
        var taskCount = tasks.Count;

        var actors = tasks
            .SelectMany(_ => _.Actors)
            .Distinct()
            .ToList();

        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : TitleHeight;

        var sectionBarY = options.Padding + titleOffset;
        var taskY = sectionBarY + SectionBarHeight + SectionTaskGap;
        var taskBottom = taskY + TaskHeight;
        var axisY = taskBottom + AxisGap;

        var lastTaskRight = TaskX(taskCount - 1) + TaskWidth;
        var minScore = tasks.Min(_ => _.Score);
        var lowestFaceY = FaceCenterY(axisY, minScore);

        var width = lastTaskRight + RightMargin;
        var height = lowestFaceY + FaceRadius + options.Padding;

        var builder = new SvgBuilder().Size(width, height);
        builder.AddArrowMarker("journey-arrow", "#333");

        // Title
        if (!string.IsNullOrEmpty(model.Title))
        {
            builder.AddText(
                LeftMargin,
                options.Padding + TitleHeight / 2,
                model.Title,
                anchor: "start",
                baseline: "middle",
                fontSize: options.FontSize + 6,
                fontFamily: options.FontFamily,
                fontWeight: "bold",
                fill: "#333");
        }

        // Actor legend (left margin)
        for (var actorIndex = 0; actorIndex < actors.Count; actorIndex++)
        {
            var legendY = sectionBarY + SectionBarHeight / 2 + actorIndex * 24;
            var actorColor = ActorColors[actorIndex % ActorColors.Length];
            builder.AddCircle(24, legendY, 7, fill: actorColor, stroke: "#333", strokeWidth: 1);
            builder.AddText(
                40,
                legendY,
                actors[actorIndex],
                anchor: "start",
                baseline: "middle",
                fontSize: options.FontSize,
                fontFamily: options.FontFamily,
                fill: "#333");
        }

        // Sections: header bar spans its tasks, then the task boxes and drop-lines below.
        var globalIndex = 0;
        for (var sectionIndex = 0; sectionIndex < sections.Count; sectionIndex++)
        {
            var section = sections[sectionIndex];
            var fill = SectionFills[sectionIndex % SectionFills.Length];
            var border = SectionBorders[sectionIndex % SectionBorders.Length];

            var firstTaskX = TaskX(globalIndex);
            var sectionRight = TaskX(globalIndex + section.Tasks.Count - 1) + TaskWidth;
            var sectionWidth = sectionRight - firstTaskX;

            builder.AddRect(
                firstTaskX,
                sectionBarY,
                sectionWidth,
                SectionBarHeight,
                rx: CornerRadius,
                fill: fill,
                stroke: "none");

            if (!string.IsNullOrEmpty(section.Name))
            {
                builder.AddText(
                    firstTaskX + sectionWidth / 2,
                    sectionBarY + SectionBarHeight / 2,
                    section.Name,
                    anchor: "middle",
                    baseline: "middle",
                    fontSize: options.FontSize,
                    fontFamily: options.FontFamily,
                    fontWeight: "bold",
                    fill: "#333");
            }

            foreach (var task in section.Tasks)
            {
                var taskX = TaskX(globalIndex);
                var centerX = taskX + TaskWidth / 2;

                builder.AddRect(
                    taskX,
                    taskY,
                    TaskWidth,
                    TaskHeight,
                    rx: CornerRadius,
                    fill: TaskFill,
                    stroke: border,
                    strokeWidth: 2);

                builder.AddText(
                    centerX,
                    taskY + TaskHeight / 2,
                    task.Name,
                    anchor: "middle",
                    baseline: "middle",
                    fontSize: options.FontSize - 1,
                    fontFamily: options.FontFamily,
                    fill: "#333");

                // Actor dots along the top edge, from the top-left corner.
                for (var taskActorIndex = 0; taskActorIndex < task.Actors.Count; taskActorIndex++)
                {
                    var actorColor = ActorColors[actors.IndexOf(task.Actors[taskActorIndex]) % ActorColors.Length];
                    builder.AddCircle(
                        taskX + 14 + taskActorIndex * 15,
                        taskY,
                        6,
                        fill: actorColor,
                        stroke: "#333",
                        strokeWidth: 1);
                }

                // Dashed drop-line from the task down past the axis to its score face.
                var faceCenterY = FaceCenterY(axisY, task.Score);
                builder.AddLine(
                    centerX,
                    taskBottom,
                    centerX,
                    faceCenterY - FaceRadius,
                    stroke: "#999",
                    strokeWidth: 1,
                    strokeDasharray: "3,3");

                globalIndex++;
            }
        }

        // Timeline axis, drawn over the drop-lines.
        builder.AddPath(
            string.Create(
                CultureInfo.InvariantCulture,
                $"M {LeftMargin:0.##} {axisY:0.##} L {lastTaskRight + 25:0.##} {axisY:0.##}"),
            fill: "none",
            stroke: "#333",
            strokeWidth: 3,
            markerEnd: "url(#journey-arrow)");

        // Score faces, drawn last so they sit above the axis and drop-lines.
        for (var index = 0; index < tasks.Count; index++)
        {
            var centerX = TaskX(index) + TaskWidth / 2;
            DrawFace(builder, centerX, FaceCenterY(axisY, tasks[index].Score), tasks[index].Score);
        }

        return builder.Build();
    }

    static double TaskX(int index) => LeftMargin + index * (TaskWidth + TaskGap);

    static double FaceCenterY(double axisY, int score) => axisY + FaceGap + (5 - score) * FaceStep;

    static void DrawFace(SvgBuilder builder, double cx, double cy, int score)
    {
        builder.AddCircle(cx, cy, FaceRadius, fill: FaceFill, stroke: FaceStroke, strokeWidth: 1);

        builder.AddCircle(cx - 5, cy - 3, 1.6, fill: FaceFeature);
        builder.AddCircle(cx + 5, cy - 3, 1.6, fill: FaceFeature);

        // Mouth tracks the score: smile (>= 4), flat (3), frown (<= 2).
        var mouth = score switch
        {
            >= 4 => string.Create(
                CultureInfo.InvariantCulture,
                $"M {cx - 6:0.##} {cy + 2:0.##} Q {cx:0.##} {cy + 8:0.##} {cx + 6:0.##} {cy + 2:0.##}"),
            <= 2 => string.Create(
                CultureInfo.InvariantCulture,
                $"M {cx - 6:0.##} {cy + 6:0.##} Q {cx:0.##} {cy:0.##} {cx + 6:0.##} {cy + 6:0.##}"),
            _ => string.Create(
                CultureInfo.InvariantCulture,
                $"M {cx - 6:0.##} {cy + 4:0.##} L {cx + 6:0.##} {cy + 4:0.##}")
        };

        builder.AddPath(mouth, fill: "none", stroke: FaceFeature, strokeWidth: 1.5);
    }
}
