using System.Globalization;
using MermaidSharp.Core;
using MermaidSharp.Core.Rendering;

namespace MermaidSharp.Diagrams.Kanban;

public class KanbanRenderer : IDiagramRenderer<KanbanModel>
{
    const double ColumnWidth = 180;
    const double ColumnPadding = 15;
    const double TaskHeight = 40;
    const double TaskPadding = 8;
    const double HeaderHeight = 40;
    const double TitleHeight = 40;

    static readonly string[] ColumnColors =
    [
        "#E3F2FD", "#E8F5E9", "#FFF3E0", "#F3E5F5",
        "#FCE4EC", "#E0F7FA", "#FFF8E1", "#F1F8E9"
    ];

    static readonly string[] TaskColors =
    [
        "#BBDEFB", "#C8E6C9", "#FFE0B2", "#E1BEE7",
        "#F8BBD0", "#B2EBF2", "#FFECB3", "#DCEDC8"
    ];

    public SvgDocument Render(KanbanModel model, RenderOptions options)
    {
        if (model.Columns.Count == 0)
        {
            var emptyBuilder = new SvgBuilder().Size(200, 100);
            emptyBuilder.AddText(100, 50, "Empty board", anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily);
            return emptyBuilder.Build();
        }

        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : TitleHeight;
        var maxTasks = model.Columns.Max(c => c.Tasks.Count);
        var contentHeight = HeaderHeight + maxTasks * (TaskHeight + TaskPadding) + ColumnPadding * 2;

        var width = model.Columns.Count * (ColumnWidth + ColumnPadding) + options.Padding * 2;
        var height = contentHeight + options.Padding * 2 + titleOffset;

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

        // Draw columns
        for (int i = 0; i < model.Columns.Count; i++)
        {
            var column = model.Columns[i];
            var x = options.Padding + i * (ColumnWidth + ColumnPadding);
            var y = options.Padding + titleOffset;

            var columnColor = ColumnColors[i % ColumnColors.Length];
            var taskColor = TaskColors[i % TaskColors.Length];

            // Column background
            builder.AddRect(x, y, ColumnWidth, contentHeight,
                rx: 8, fill: columnColor, stroke: "#ccc", strokeWidth: 1);

            // Column header
            builder.AddRect(x, y, ColumnWidth, HeaderHeight,
                rx: 8, fill: columnColor, stroke: "none");
            builder.AddRect(x, y + HeaderHeight - 8, ColumnWidth, 8,
                fill: columnColor, stroke: "none");

            builder.AddText(x + ColumnWidth / 2, y + HeaderHeight / 2, column.Name,
                anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily,
                fontWeight: "bold", fill: "#333");

            // Tasks
            for (int j = 0; j < column.Tasks.Count; j++)
            {
                var task = column.Tasks[j];
                var taskX = x + TaskPadding;
                var taskY = y + HeaderHeight + ColumnPadding + j * (TaskHeight + TaskPadding);
                var taskWidth = ColumnWidth - TaskPadding * 2;

                // Task card
                builder.AddRect(taskX, taskY, taskWidth, TaskHeight,
                    rx: 4, fill: "#fff", stroke: "#ddd", strokeWidth: 1);

                // Color bar on left
                builder.AddRect(taskX, taskY, 4, TaskHeight,
                    rx: 2, fill: taskColor, stroke: "none");

                // Task text
                builder.AddText(taskX + 12, taskY + TaskHeight / 2, task.Name,
                    anchor: "start", baseline: "middle",
                    fontSize: $"{options.FontSize - 2}px", fontFamily: options.FontFamily,
                    fill: "#333");
            }
        }

        return builder.Build();
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
