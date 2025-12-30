namespace MermaidSharp.Diagrams.C4;

public class C4Renderer : IDiagramRenderer<C4Model>
{
    const double ElementWidth = 160;
    const double ElementHeight = 100;
    const double PersonHeight = 120;
    const double ElementSpacing = 40;
    const double TitleHeight = 50;
    const double RowSpacing = 60;

    const string PersonColor = "#08427B";
    const string PersonExtColor = "#999999";
    const string SystemColor = "#1168BD";
    const string SystemExtColor = "#999999";
    const string ContainerColor = "#438DD5";
    const string ContainerDbColor = "#438DD5";
    const string ComponentColor = "#85BBF0";

    public SvgDocument Render(C4Model model, RenderOptions options)
    {
        if (model.Elements.Count == 0)
        {
            var emptyBuilder = new SvgBuilder().Size(200, 100);
            emptyBuilder.AddText(100, 50, "Empty C4 diagram", anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily);
            return emptyBuilder.Build();
        }

        // Group elements by type for layout
        var persons = model.Elements.Where(e => e.Type == C4ElementType.Person).ToList();
        var systems = model.Elements.Where(e => e.Type == C4ElementType.System).ToList();
        var containers = model.Elements.Where(e =>
            e.Type is C4ElementType.Container or C4ElementType.ContainerDb or C4ElementType.ContainerQueue).ToList();
        var components = model.Elements.Where(e => e.Type == C4ElementType.Component).ToList();

        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : TitleHeight;

        // Calculate layout
        var maxPerRow = 4;
        var personRows = (int) Math.Ceiling((double) persons.Count / maxPerRow);
        var systemRows = (int) Math.Ceiling((double) systems.Count / maxPerRow);
        var containerRows = (int) Math.Ceiling((double) containers.Count / maxPerRow);
        var componentRows = (int) Math.Ceiling((double) components.Count / maxPerRow);

        var totalRows = personRows + systemRows + containerRows + componentRows;
        var maxCols = Math.Max(1, new[] {persons.Count, systems.Count, containers.Count, components.Count}.Max());
        maxCols = Math.Min(maxCols, maxPerRow);

        var width = maxCols * (ElementWidth + ElementSpacing) + options.Padding * 2;
        var height = titleOffset + totalRows * (ElementHeight + RowSpacing) + options.Padding * 2 + 50;

        var builder = new SvgBuilder().Size(width, height);

        // Add arrow marker
        builder.AddArrowMarker("c4arrow", "#666");

        // Draw title
        if (!string.IsNullOrEmpty(model.Title))
        {
            builder.AddText(width / 2, options.Padding + TitleHeight / 2, model.Title,
                anchor: "middle",
                baseline: "middle",
                fontSize: $"{options.FontSize + 6}px",
                fontFamily: options.FontFamily,
                fontWeight: "bold");
        }

        // Position tracking
        var elementPositions = new Dictionary<string, (double x, double y, double w, double h)>();
        var currentY = options.Padding + titleOffset;

        // Draw persons
        currentY = DrawElementRow(builder, persons, currentY, width, options, elementPositions, "person");

        // Draw systems
        currentY = DrawElementRow(builder, systems, currentY, width, options, elementPositions, "system");

        // Draw containers
        currentY = DrawElementRow(builder, containers, currentY, width, options, elementPositions, "container");

        // Draw components
        currentY = DrawElementRow(builder, components, currentY, width, options, elementPositions, "component");

        // Draw relationships
        foreach (var rel in model.Relationships)
        {
            if (elementPositions.TryGetValue(rel.From, out var fromPos) &&
                elementPositions.TryGetValue(rel.To, out var toPos))
            {
                DrawRelationship(builder, fromPos, toPos, rel.Label, options);
            }
        }

        return builder.Build();
    }

    static double DrawElementRow(SvgBuilder builder, List<C4Element> elements, double startY, double totalWidth,
        RenderOptions options, Dictionary<string, (double x, double y, double w, double h)> positions, string type)
    {
        if (elements.Count == 0) return startY;

        var rowWidth = elements.Count * (ElementWidth + ElementSpacing) - ElementSpacing;
        var startX = (totalWidth - rowWidth) / 2;

        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            var x = startX + i * (ElementWidth + ElementSpacing);
            var h = element.Type == C4ElementType.Person ? PersonHeight : ElementHeight;

            positions[element.Id] = (x + ElementWidth / 2, startY + h / 2, ElementWidth, h);

            DrawElement(builder, element, x, startY, options);
        }

        var maxHeight = elements.Max(e => e.Type == C4ElementType.Person ? PersonHeight : ElementHeight);
        return startY + maxHeight + RowSpacing;
    }

    static void DrawElement(SvgBuilder builder, C4Element element, double x, double y, RenderOptions options)
    {
        var color = GetElementColor(element);
        var textColor = "#FFFFFF";

        if (element.Type == C4ElementType.Person)
        {
            // Draw person shape (head + body)
            var headRadius = 20;
            var bodyHeight = 60;
            var bodyWidth = 80;

            // Head
            builder.AddCircle(x + ElementWidth / 2, y + headRadius + 5, headRadius,
                fill: color, stroke: "none");

            // Body (rounded rect)
            builder.AddRect(x + (ElementWidth - bodyWidth) / 2, y + headRadius * 2 + 10,
                bodyWidth, bodyHeight, rx: 10,
                fill: color, stroke: "none");

            // Label
            builder.AddText(x + ElementWidth / 2, y + PersonHeight - 20, element.Label,
                anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize - 1}px", fontFamily: options.FontFamily,
                fill: textColor, fontWeight: "bold");

            // Description
            if (!string.IsNullOrEmpty(element.Description))
            {
                builder.AddText(x + ElementWidth / 2, y + PersonHeight - 5,
                    TruncateText(element.Description, 25),
                    anchor: "middle", baseline: "middle",
                    fontSize: $"{options.FontSize - 3}px", fontFamily: options.FontFamily,
                    fill: textColor);
            }
        }
        else if (element.Type == C4ElementType.ContainerDb)
        {
            // Draw database shape (cylinder)
            var ellipseHeight = 15;

            // Top ellipse
            builder.AddEllipse(x + ElementWidth / 2, y + ellipseHeight,
                ElementWidth / 2 - 5, ellipseHeight,
                fill: color, stroke: "none");

            // Body
            builder.AddRect(x + 5, y + ellipseHeight, ElementWidth - 10, ElementHeight - ellipseHeight * 2,
                fill: color, stroke: "none");

            // Bottom ellipse
            builder.AddEllipse(x + ElementWidth / 2, y + ElementHeight - ellipseHeight,
                ElementWidth / 2 - 5, ellipseHeight,
                fill: color, stroke: "none");

            DrawElementText(builder, element, x, y, options, textColor);
        }
        else
        {
            // Standard box
            builder.AddRect(x, y, ElementWidth, ElementHeight, rx: 5,
                fill: color, stroke: "none");

            DrawElementText(builder, element, x, y, options, textColor);
        }
    }

    static void DrawElementText(SvgBuilder builder, C4Element element, double x, double y,
        RenderOptions options, string textColor)
    {
        var centerX = x + ElementWidth / 2;
        var textY = y + 25;

        // Label
        builder.AddText(centerX, textY, element.Label,
            anchor: "middle", baseline: "middle",
            fontSize: $"{options.FontSize - 1}px", fontFamily: options.FontFamily,
            fill: textColor, fontWeight: "bold");

        // Technology
        if (!string.IsNullOrEmpty(element.Technology))
        {
            textY += 18;
            builder.AddText(centerX, textY, $"[{element.Technology}]",
                anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize - 3}px", fontFamily: options.FontFamily,
                fill: textColor);
        }

        // Description
        if (!string.IsNullOrEmpty(element.Description))
        {
            textY += 18;
            builder.AddText(centerX, textY, TruncateText(element.Description, 22),
                anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize - 3}px", fontFamily: options.FontFamily,
                fill: textColor);
        }
    }

    static void DrawRelationship(SvgBuilder builder,
        (double x, double y, double w, double h) from,
        (double x, double y, double w, double h) to,
        string? label, RenderOptions options)
    {
        // Calculate connection points
        var dx = to.x - from.x;
        var dy = to.y - from.y;
        var angle = Math.Atan2(dy, dx);

        var fromX = from.x + Math.Cos(angle) * from.w / 2;
        var fromY = from.y + Math.Sin(angle) * from.h / 2;
        var toX = to.x - Math.Cos(angle) * to.w / 2;
        var toY = to.y - Math.Sin(angle) * to.h / 2;

        // Draw line
        builder.AddLine(fromX, fromY, toX, toY,
            stroke: "#666", strokeWidth: 1.5, strokeDasharray: "5,5");

        // Draw arrowhead manually
        var arrowSize = 8;
        var arrowAngle = Math.PI / 6;
        var ax1 = toX - arrowSize * Math.Cos(angle - arrowAngle);
        var ay1 = toY - arrowSize * Math.Sin(angle - arrowAngle);
        var ax2 = toX - arrowSize * Math.Cos(angle + arrowAngle);
        var ay2 = toY - arrowSize * Math.Sin(angle + arrowAngle);

        builder.AddPath($"M {Fmt(toX)} {Fmt(toY)} L {Fmt(ax1)} {Fmt(ay1)} L {Fmt(ax2)} {Fmt(ay2)} Z",
            fill: "#666", stroke: "none");

        // Draw label
        if (!string.IsNullOrEmpty(label))
        {
            var midX = (fromX + toX) / 2;
            var midY = (fromY + toY) / 2;

            builder.AddText(midX, midY - 8, label,
                anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize - 3}px", fontFamily: options.FontFamily,
                fill: "#666");
        }
    }

    static string GetElementColor(C4Element element)
    {
        if (element.IsExternal)
        {
            return element.Type == C4ElementType.Person ? PersonExtColor : SystemExtColor;
        }

        return element.Type switch
        {
            C4ElementType.Person => PersonColor,
            C4ElementType.System => SystemColor,
            C4ElementType.Container => ContainerColor,
            C4ElementType.ContainerDb => ContainerDbColor,
            C4ElementType.ContainerQueue => ContainerColor,
            C4ElementType.Component => ComponentColor,
            _ => SystemColor
        };
    }

    static string TruncateText(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        return string.Concat(text.AsSpan(0, maxLength - 3), "...");
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}