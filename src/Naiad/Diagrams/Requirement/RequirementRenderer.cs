namespace MermaidSharp.Diagrams.Requirement;

public class RequirementRenderer : IDiagramRenderer<RequirementModel>
{
    const double BoxWidth = 180;
    const double BoxHeight = 80;
    const double BoxSpacing = 60;
    const double TitleHeight = 40;

    const string RequirementColor = "#C8E6C9";
    const string ElementColor = "#BBDEFB";

    public SvgDocument Render(RequirementModel model, RenderOptions options)
    {
        if (model.Requirements.Count == 0 && model.Elements.Count == 0)
        {
            var emptyBuilder = new SvgBuilder().Size(200, 100);
            emptyBuilder.AddText(100, 50, "Empty diagram", anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily);
            return emptyBuilder.Build();
        }

        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : TitleHeight;

        // Layout: requirements on left, elements on right
        var maxItems = Math.Max(model.Requirements.Count, model.Elements.Count);
        var height = maxItems * (BoxHeight + BoxSpacing) + options.Padding * 2 + titleOffset;
        var width = 2 * (BoxWidth + BoxSpacing) + options.Padding * 2;

        var builder = new SvgBuilder().Size(width, height);

        // Add arrow marker
        builder.AddArrowMarker("reqarrow", "#666");

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

        // Track positions
        var positions = new Dictionary<string, (double x, double y)>();

        // Draw requirements (left column)
        var reqX = options.Padding;
        for (var i = 0; i < model.Requirements.Count; i++)
        {
            var req = model.Requirements[i];
            var y = options.Padding + titleOffset + i * (BoxHeight + BoxSpacing);

            positions[req.Name] = (reqX + BoxWidth / 2, y + BoxHeight / 2);
            DrawRequirement(builder, req, reqX, y, options);
        }

        // Draw elements (right column)
        var elemX = options.Padding + BoxWidth + BoxSpacing;
        for (var i = 0; i < model.Elements.Count; i++)
        {
            var elem = model.Elements[i];
            var y = options.Padding + titleOffset + i * (BoxHeight + BoxSpacing);

            positions[elem.Name] = (elemX + BoxWidth / 2, y + BoxHeight / 2);
            DrawElement(builder, elem, elemX, y, options);
        }

        // Draw relations
        foreach (var rel in model.Relations)
        {
            if (positions.TryGetValue(rel.Source, out var from) &&
                positions.TryGetValue(rel.Target, out var to))
            {
                DrawRelation(builder, from, to, rel.Type, options);
            }
        }

        return builder.Build();
    }

    static void DrawRequirement(SvgBuilder builder, Requirement req, double x, double y, RenderOptions options)
    {
        // Box
        builder.AddRect(x, y, BoxWidth, BoxHeight, rx: 5,
            fill: RequirementColor, stroke: "#4CAF50", strokeWidth: 2);

        // Type label
        var typeLabel = req.Type switch
        {
            RequirementType.FunctionalRequirement => "Functional",
            RequirementType.InterfaceRequirement => "Interface",
            RequirementType.PerformanceRequirement => "Performance",
            RequirementType.PhysicalRequirement => "Physical",
            RequirementType.DesignConstraint => "Constraint",
            _ => "Requirement"
        };

        builder.AddText(x + BoxWidth / 2, y + 15, $"<<{typeLabel}>>",
            anchor: "middle", baseline: "middle",
            fontSize: $"{options.FontSize - 3}px", fontFamily: options.FontFamily,
            fill: "#666");

        // Name
        builder.AddText(x + BoxWidth / 2, y + 35, req.Name,
            anchor: "middle", baseline: "middle",
            fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily,
            fontWeight: "bold", fill: "#333");

        // Risk indicator
        var riskColor = req.Risk switch
        {
            RiskLevel.Low => "#4CAF50",
            RiskLevel.High => "#F44336",
            _ => "#FF9800"
        };
        builder.AddCircle(x + 15, y + BoxHeight - 15, 6, fill: riskColor, stroke: "#333", strokeWidth: 1);

        // Text (truncated)
        if (!string.IsNullOrEmpty(req.Text))
        {
            var text = req.Text.Length > 25 ? string.Concat(req.Text.AsSpan(0, 22), "...") : req.Text;
            builder.AddText(x + BoxWidth / 2, y + BoxHeight - 15, text,
                anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize - 3}px", fontFamily: options.FontFamily,
                fill: "#666");
        }
    }

    static void DrawElement(SvgBuilder builder, RequirementElement elem, double x, double y, RenderOptions options)
    {
        // Box
        builder.AddRect(x, y, BoxWidth, BoxHeight, rx: 5,
            fill: ElementColor, stroke: "#2196F3", strokeWidth: 2);

        // Type label
        builder.AddText(x + BoxWidth / 2, y + 15, "<<Element>>",
            anchor: "middle", baseline: "middle",
            fontSize: $"{options.FontSize - 3}px", fontFamily: options.FontFamily,
            fill: "#666");

        // Name
        builder.AddText(x + BoxWidth / 2, y + 35, elem.Name,
            anchor: "middle", baseline: "middle",
            fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily,
            fontWeight: "bold", fill: "#333");

        // Type
        if (!string.IsNullOrEmpty(elem.Type))
        {
            builder.AddText(x + BoxWidth / 2, y + 55, $"Type: {elem.Type}",
                anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize - 3}px", fontFamily: options.FontFamily,
                fill: "#666");
        }
    }

    static void DrawRelation(
        SvgBuilder builder,
        (double x, double y) from,
        (double x, double y) to,
        RelationType type,
        RenderOptions options)
    {
        // Calculate edge points
        var dx = to.x - from.x;
        var dy = to.y - from.y;
        var angle = Math.Atan2(dy, dx);

        var fromX = from.x + Math.Cos(angle) * BoxWidth / 2;
        var fromY = from.y + Math.Sin(angle) * BoxHeight / 2;
        var toX = to.x - Math.Cos(angle) * BoxWidth / 2;
        var toY = to.y - Math.Sin(angle) * BoxHeight / 2;

        // Draw line
        builder.AddLine(fromX, fromY, toX, toY,
            stroke: "#666", strokeWidth: 1.5);

        // Draw arrowhead
        var arrowSize = 8;
        var arrowAngle = Math.PI / 6;
        var ax1 = toX - arrowSize * Math.Cos(angle - arrowAngle);
        var ay1 = toY - arrowSize * Math.Sin(angle - arrowAngle);
        var ax2 = toX - arrowSize * Math.Cos(angle + arrowAngle);
        var ay2 = toY - arrowSize * Math.Sin(angle + arrowAngle);

        builder.AddPath($"M {Fmt(toX)} {Fmt(toY)} L {Fmt(ax1)} {Fmt(ay1)} L {Fmt(ax2)} {Fmt(ay2)} Z",
            fill: "#666", stroke: "none");

        // Draw label
        var midX = (fromX + toX) / 2;
        var midY = (fromY + toY) / 2;
        var label = type.ToString().ToLowerInvariant();

        builder.AddText(midX, midY - 8, $"<<{label}>>",
            anchor: "middle", baseline: "middle",
            fontSize: $"{options.FontSize - 3}px", fontFamily: options.FontFamily,
            fill: "#666");
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
