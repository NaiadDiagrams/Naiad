namespace MermaidSharp.Diagrams.Gantt;

public class GanttRenderer : IDiagramRenderer<GanttModel>
{
    const double rowHeight = 30;
    const double taskBarHeight = 20;
    const double sectionHeaderHeight = 25;
    const double axisHeight = 40;
    const double leftMargin = 150;
    const double dayWidth = 20;
    const double milestoneSize = 12;

    const string taskColor = "#4CAF50";
    const string taskDoneColor = "#808080";
    const string taskActiveColor = "#2196F3";
    const string taskCritColor = "#F44336";
    const string sectionColor = "#ECECFF";
    const string milestoneColor = "#FF9800";

    public SvgDocument Render(GanttModel model, RenderOptions options)
    {
        // Compute task dates
        var tasks = ComputeTaskDates(model);

        if (tasks.Count == 0)
        {
            // Empty chart
            var emptyBuilder = new SvgBuilder().Size(200, 100);
            emptyBuilder.AddText(100, 50, "No tasks", anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily);
            return emptyBuilder.Build();
        }

        // Calculate date range
        var minDate = tasks.Min(_ => _.ComputedStart);
        var maxDate = tasks.Max(_ => _.ComputedEnd);
        var totalDays = (maxDate - minDate).Days + 1;

        // Calculate dimensions
        var totalRows = 0;
        foreach (var section in model.Sections)
        {
            if (!string.IsNullOrEmpty(section.Name))
                totalRows++; // Section header
            totalRows += section.Tasks.Count;
        }

        var chartWidth = totalDays * dayWidth;
        var chartHeight = totalRows * rowHeight + axisHeight;

        var width = leftMargin + chartWidth + options.Padding * 2;
        var height = chartHeight + options.Padding * 2;

        var builder = new SvgBuilder().Size(width, height);

        var offsetX = options.Padding + leftMargin;
        var offsetY = options.Padding;

        // Draw title
        if (!string.IsNullOrEmpty(model.Title))
        {
            builder.AddText(width / 2, offsetY + 15, model.Title,
                anchor: "middle",
                baseline: "middle",
                fontSize: $"{options.FontSize + 2}px",
                fontFamily: options.FontFamily);
            offsetY += 30;
        }

        // Draw axis
        DrawAxis(builder, minDate, totalDays, offsetX, offsetY, chartWidth, options);
        offsetY += axisHeight;

        // Draw grid lines
        DrawGridLines(builder, minDate, totalDays, offsetX, offsetY, chartWidth, totalRows * rowHeight);

        // Draw sections and tasks
        var currentRow = 0;
        foreach (var section in model.Sections)
        {
            // Section header
            if (!string.IsNullOrEmpty(section.Name))
            {
                var sectionY = offsetY + currentRow * rowHeight;
                builder.AddRect(options.Padding, sectionY, leftMargin + chartWidth, sectionHeaderHeight,
                    fill: sectionColor, stroke: "none");
                builder.AddText(options.Padding + 10, sectionY + sectionHeaderHeight / 2, section.Name,
                    anchor: "start",
                    baseline: "middle",
                    fontSize: $"{options.FontSize}px",
                    fontFamily: options.FontFamily,
                    fontWeight: "bold");
                currentRow++;
            }

            // Tasks
            foreach (var task in section.Tasks)
            {
                DrawTask(builder, task, minDate, currentRow, offsetX, offsetY, options);
                currentRow++;
            }
        }

        return builder.Build();
    }

    static void DrawAxis(SvgBuilder builder, DateTime startDate, int totalDays, double offsetX, double offsetY,
        double chartWidth, RenderOptions options)
    {
        // Axis line
        builder.AddLine(offsetX, offsetY + axisHeight - 5, offsetX + chartWidth, offsetY + axisHeight - 5,
            stroke: "#333", strokeWidth: 1);

        // Date labels (show every few days based on scale)
        var interval = totalDays > 30 ? 7 : totalDays > 14 ? 3 : 1;
        for (var i = 0; i < totalDays; i += interval)
        {
            var x = offsetX + i * dayWidth;
            var date = startDate.AddDays(i);

            // Tick mark
            builder.AddLine(x, offsetY + axisHeight - 10, x, offsetY + axisHeight - 5,
                stroke: "#333", strokeWidth: 1);

            // Date label
            var label = date.ToString("MM/dd", CultureInfo.InvariantCulture);
            builder.AddText(x, offsetY + axisHeight - 20, label,
                anchor: "middle",
                baseline: "middle",
                fontSize: $"{options.FontSize - 2}px",
                fontFamily: options.FontFamily,
                fill: "#666");
        }
    }

    static void DrawGridLines(SvgBuilder builder, DateTime startDate, int totalDays, double offsetX, double offsetY,
        double chartWidth, double chartHeight)
    {
        // Vertical grid lines (weekly)
        for (var i = 0; i < totalDays; i++)
        {
            var date = startDate.AddDays(i);
            if (date.DayOfWeek != DayOfWeek.Monday && i != 0)
            {
                continue;
            }

            var x = offsetX + i * dayWidth;
            builder.AddLine(x, offsetY, x, offsetY + chartHeight, stroke: "#ddd", strokeWidth: 1);
        }

        // Horizontal grid lines
        var numRows = (int)(chartHeight / rowHeight);
        for (var i = 0; i <= numRows; i++)
        {
            var y = offsetY + i * rowHeight;
            builder.AddLine(offsetX, y, offsetX + chartWidth, y,
                stroke: "#eee", strokeWidth: 1);
        }
    }

    static void DrawTask(SvgBuilder builder, GanttTask task, DateTime startDate, int row,
        double offsetX, double offsetY, RenderOptions options)
    {
        var y = offsetY + row * rowHeight;
        var startDays = (task.ComputedStart - startDate).Days;
        var durationDays = Math.Max(1, (task.ComputedEnd - task.ComputedStart).Days);

        var taskX = offsetX + startDays * dayWidth;
        var taskWidth = durationDays * dayWidth;

        // Task name on the left
        builder.AddText(options.Padding + 10, y + rowHeight / 2, task.Name,
            anchor: "start",
            baseline: "middle",
            fontSize: $"{options.FontSize}px",
            fontFamily: options.FontFamily);

        // Determine task color
        var color = task.Status switch
        {
            GanttTaskStatus.Done => taskDoneColor,
            GanttTaskStatus.Active => taskActiveColor,
            _ => task.IsCritical ? taskCritColor : taskColor
        };

        if (task.IsMilestone)
        {
            // Draw milestone as diamond
            var cx = taskX;
            var cy = y + rowHeight / 2;
            var path = $"M {Fmt(cx)} {Fmt(cy - milestoneSize)} " +
                       $"L {Fmt(cx + milestoneSize)} {Fmt(cy)} " +
                       $"L {Fmt(cx)} {Fmt(cy + milestoneSize)} " +
                       $"L {Fmt(cx - milestoneSize)} {Fmt(cy)} Z";
            builder.AddPath(path, fill: milestoneColor, stroke: "#333", strokeWidth: 1);
        }
        else
        {
            // Draw task bar
            var barY = y + (rowHeight - taskBarHeight) / 2;
            builder.AddRect(taskX, barY, taskWidth, taskBarHeight,
                rx: 3,
                fill: color,
                stroke: "#333",
                strokeWidth: 1);

            // Task ID or duration inside bar if fits
            if (task.Id != null && taskWidth > 40)
            {
                builder.AddText(taskX + taskWidth / 2, barY + taskBarHeight / 2, task.Id,
                    anchor: "middle",
                    baseline: "middle",
                    fontSize: $"{options.FontSize - 2}px",
                    fontFamily: options.FontFamily,
                    fill: "#fff");
            }
        }
    }

    static List<GanttTask> ComputeTaskDates(GanttModel model)
    {
        var allTasks = model.Sections.SelectMany(_ => _.Tasks).ToList();
        var taskMap = allTasks.Where(_ => _.Id != null).ToDictionary(_ => _.Id!, _ => _);

        // Default start date if none specified
        var defaultStart = DateTime.Today;

        // First pass: compute tasks without dependencies
        foreach (var task in allTasks)
        {
            if (task.StartDate.HasValue)
            {
                task.ComputedStart = task.StartDate.Value;
            }
            else if (string.IsNullOrEmpty(task.AfterTaskId))
            {
                task.ComputedStart = defaultStart;
            }

            if (task.EndDate.HasValue)
            {
                task.ComputedEnd = task.EndDate.Value;
            }
            else if (task.Duration.HasValue)
            {
                task.ComputedEnd = task.ComputedStart.Add(task.Duration.Value);
            }
            else
            {
                task.ComputedEnd = task.ComputedStart.AddDays(1);
            }
        }

        // Second pass: resolve dependencies
        var changed = true;
        var maxIterations = 100;
        while (changed && maxIterations-- > 0)
        {
            changed = false;
            foreach (var task in allTasks)
            {
                if (!string.IsNullOrEmpty(task.AfterTaskId))
                {
                    if (taskMap.TryGetValue(task.AfterTaskId, out var dependsOn))
                    {
                        var newStart = dependsOn.ComputedEnd;
                        if (newStart != task.ComputedStart)
                        {
                            task.ComputedStart = newStart;
                            if (task.Duration.HasValue)
                            {
                                task.ComputedEnd = task.ComputedStart.Add(task.Duration.Value);
                            }
                            else if (!task.EndDate.HasValue)
                            {
                                task.ComputedEnd = task.ComputedStart.AddDays(1);
                            }
                            changed = true;
                        }
                    }
                }
            }
        }

        return allTasks;
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
