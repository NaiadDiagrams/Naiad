namespace Naiad.Diagrams.Kanban;

public class KanbanRenderer : IDiagramRenderer<KanbanModel>
{
    const double columnWidth = 180;
    const double columnPadding = 15;
    const double taskHeight = 40;
    const double taskPadding = 8;
    const double headerHeight = 40;
    const double titleHeight = 40;

    static string[] columnColors =
    [
        "#E3F2FD", "#E8F5E9", "#FFF3E0", "#F3E5F5",
        "#FCE4EC", "#E0F7FA", "#FFF8E1", "#F1F8E9"
    ];

    static string[] taskColors =
    [
        "#BBDEFB", "#C8E6C9", "#FFE0B2", "#E1BEE7",
        "#F8BBD0", "#B2EBF2", "#FFECB3", "#DCEDC8"
    ];

    public SvgDocument Render(KanbanModel model, RenderOptions options)
    {
        if (model.Columns.Count == 0)
        {
            var emptyBuilder = new SvgBuilder();
            emptyBuilder.Size(200, 100);
            emptyBuilder.AddText(
                100,
                50,
                "Empty board",
                anchor: "middle",
                baseline: "middle",
                fontSize: options.FontSize,
                fontFamily: options.FontFamily);
            return emptyBuilder.Build();
        }

        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : titleHeight;
        var maxTasks = model.Columns.Max(_ => _.Tasks.Count);
        var contentHeight = headerHeight + maxTasks * (taskHeight + taskPadding) + columnPadding * 2;

        var width = model.Columns.Count * (columnWidth + columnPadding) + options.Padding * 2;
        var height = contentHeight + options.Padding * 2 + titleOffset;

        var builder = new SvgBuilder();
        builder.Size(width, height);

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

        // Draw columns
        for (var i = 0; i < model.Columns.Count; i++)
        {
            var column = model.Columns[i];
            var x = options.Padding + i * (columnWidth + columnPadding);
            var y = options.Padding + titleOffset;

            var columnColor = columnColors[i % columnColors.Length];
            var taskColor = taskColors[i % taskColors.Length];

            // Column background
            builder.AddRect(
                x,
                y,
                columnWidth,
                contentHeight,
                rx: 8,
                fill: columnColor,
                stroke: "#ccc",
                strokeWidth: 1);

            // Column header
            builder.AddRect(
                x,
                y,
                columnWidth,
                headerHeight,
                rx: 8,
                fill: columnColor,
                stroke: "none");
            builder.AddRect(
                x,
                y + headerHeight - 8,
                columnWidth,
                8,
                fill: columnColor,
                stroke: "none");

            builder.AddText(
                x + columnWidth / 2,
                y + headerHeight / 2,
                column.Name,
                anchor: "middle",
                baseline: "middle",
                fontSize: options.FontSize,
                fontFamily: options.FontFamily,
                fontWeight: "bold",
                fill: "#333");

            // Tasks
            for (var j = 0; j < column.Tasks.Count; j++)
            {
                var task = column.Tasks[j];
                var taskX = x + taskPadding;
                var taskY = y + headerHeight + columnPadding + j * (taskHeight + taskPadding);
                const double taskWidth = columnWidth - taskPadding * 2;

                // Task card
                builder.AddRect(
                    taskX,
                    taskY,
                    taskWidth,
                    taskHeight,
                    rx: 4,
                    fill: "#fff",
                    stroke: "#ddd",
                    strokeWidth: 1);

                // Color bar on left
                builder.AddRect(
                    taskX,
                    taskY,
                    4,
                    taskHeight,
                    rx: 2,
                    fill: taskColor,
                    stroke: "none");

                // Task text
                builder.AddText(
                    taskX + 12,
                    taskY + taskHeight / 2,
                    task.Name,
                    anchor: "start",
                    baseline: "middle",
                    fontSize: options.FontSize - 2,
                    fontFamily: options.FontFamily,
                    fill: "#333");
            }
        }

        return builder.Build();
    }
}
