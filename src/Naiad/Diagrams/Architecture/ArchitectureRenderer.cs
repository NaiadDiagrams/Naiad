namespace MermaidSharp.Diagrams.Architecture;

public class ArchitectureRenderer : IDiagramRenderer<ArchitectureModel>
{
    const double ServiceWidth = 100;
    const double ServiceHeight = 80;
    const double ServiceSpacing = 40;
    const double GroupPadding = 20;
    const double IconSize = 32;

    static readonly Dictionary<string, string> IconPaths = new()
    {
        ["cloud"] = "M25,60 Q0,60 0,45 Q0,30 15,30 Q15,15 35,15 Q55,15 55,30 Q70,30 70,45 Q70,60 45,60 Z",
        ["database"] = "M10,20 L10,50 Q25,60 40,50 L40,20 Q25,10 10,20 M10,20 Q25,30 40,20",
        ["disk"] = "M5,40 L5,20 A20,10 0 1,1 45,20 L45,40 A20,10 0 1,1 5,40 M5,20 A20,10 0 1,0 45,20",
        ["internet"] = "M25,5 A20,20 0 1,1 25,45 A20,20 0 1,1 25,5 M5,25 L45,25 M25,5 Q15,25 25,45 M25,5 Q35,25 25,45",
        ["server"] = "M5,10 L45,10 L45,40 L5,40 Z M5,15 L45,15 M8,12.5 A1,1 0 1,1 8,12.49"
    };

    static readonly Dictionary<string, string> iconColors = new()
    {
        ["cloud"] = "#4FC3F7",
        ["database"] = "#81C784",
        ["disk"] = "#FFB74D",
        ["internet"] = "#BA68C8",
        ["server"] = "#90A4AE"
    };

    static readonly string[] GroupColors =
    [
        "#E3F2FD", "#E8F5E9", "#FFF3E0", "#F3E5F5",
        "#FCE4EC", "#E0F7FA", "#FFF8E1", "#F1F8E9"
    ];

    const double GroupLabelHeight = 24;

    public SvgDocument Render(ArchitectureModel model, RenderOptions options)
    {
        if (model.Services.Count == 0 && model.Groups.Count == 0)
        {
            var emptyBuilder = new SvgBuilder().Size(200, 100);
            emptyBuilder.AddText(100, 50, "Empty diagram", anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily);
            return emptyBuilder.Build();
        }

        // Check if any services belong to groups
        var hasGroups = model.Groups.Count > 0 && model.Services.Any(s => !string.IsNullOrEmpty(s.Parent));

        // Offset for group padding (to make room for group bounds)
        var offsetX = hasGroups ? GroupPadding : 0;
        var offsetY = hasGroups ? GroupPadding + GroupLabelHeight : 0;

        // Simple grid layout for services
        var positions = new Dictionary<string, (double x, double y)>();
        var cols = (int)Math.Ceiling(Math.Sqrt(model.Services.Count + model.Junctions.Count));
        var rows = (int)Math.Ceiling((double)(model.Services.Count + model.Junctions.Count) / cols);

        // Content dimensions (with extra space for groups if needed)
        var contentWidth = cols * ServiceWidth + Math.Max(0, cols - 1) * ServiceSpacing + (hasGroups ? GroupPadding * 2 : 0);
        var contentHeight = rows * ServiceHeight + Math.Max(0, rows - 1) * ServiceSpacing + (hasGroups ? GroupPadding * 2 + GroupLabelHeight : 0);

        var builder = new SvgBuilder()
            .Size(contentWidth, contentHeight)
            .Padding(options.Padding);

        // Add arrow marker
        builder.AddArrowMarker("arch-arrow", "#666");

        // Position services (calculate positions first, draw later)
        var servicePositions = new Dictionary<string, (double x, double y, double width, double height)>();
        var idx = 0;
        foreach (var service in model.Services)
        {
            var col = idx % cols;
            var row = idx / cols;
            var x = offsetX + col * (ServiceWidth + ServiceSpacing);
            var y = offsetY + row * (ServiceHeight + ServiceSpacing);

            positions[service.Id] = (x + ServiceWidth / 2, y + ServiceHeight / 2);
            servicePositions[service.Id] = (x, y, ServiceWidth, ServiceHeight);
            idx++;
        }

        // Position junctions
        var junctionPositions = new Dictionary<string, (double x, double y)>();
        foreach (var junction in model.Junctions)
        {
            var col = idx % cols;
            var row = idx / cols;
            var x = offsetX + col * (ServiceWidth + ServiceSpacing);
            var y = offsetY + row * (ServiceHeight + ServiceSpacing);

            positions[junction.Id] = (x + ServiceWidth / 2, y + ServiceHeight / 2);
            junctionPositions[junction.Id] = (x + ServiceWidth / 2, y + ServiceHeight / 2);
            idx++;
        }

        // Draw groups first (as background)
        var colorIndex = 0;
        foreach (var group in model.Groups)
        {
            var bounds = CalculateGroupBounds(group.Id, model.Services, servicePositions);
            if (bounds.HasValue)
            {
                var color = GroupColors[colorIndex % GroupColors.Length];
                DrawGroup(builder, group, bounds.Value, color, options);
                colorIndex++;
            }
        }

        // Draw services
        foreach (var service in model.Services)
        {
            var pos = servicePositions[service.Id];
            DrawService(builder, service, pos.x, pos.y, options);
        }

        // Draw junctions
        foreach (var junction in model.Junctions)
        {
            var pos = junctionPositions[junction.Id];
            DrawJunction(builder, junction, pos.x, pos.y, options);
        }

        // Draw edges
        foreach (var edge in model.Edges)
        {
            if (positions.TryGetValue(edge.SourceId, out var from) &&
                positions.TryGetValue(edge.TargetId, out var to))
            {
                DrawEdge(builder, from, to, edge, options);
            }
        }

        return builder.Build();
    }

    static (double x, double y, double width, double height)? CalculateGroupBounds(
        string groupId,
        List<ArchitectureService> services,
        Dictionary<string, (double x, double y, double width, double height)> servicePositions)
    {
        double? minX = null, minY = null, maxX = null, maxY = null;

        foreach (var service in services)
        {
            if (service.Parent == groupId && servicePositions.TryGetValue(service.Id, out var pos))
            {
                minX = minX.HasValue ? Math.Min(minX.Value, pos.x) : pos.x;
                minY = minY.HasValue ? Math.Min(minY.Value, pos.y) : pos.y;
                maxX = maxX.HasValue ? Math.Max(maxX.Value, pos.x + pos.width) : pos.x + pos.width;
                maxY = maxY.HasValue ? Math.Max(maxY.Value, pos.y + pos.height) : pos.y + pos.height;
            }
        }

        if (!minX.HasValue || !minY.HasValue || !maxX.HasValue || !maxY.HasValue)
            return null;

        return (
            minX.Value - GroupPadding,
            minY.Value - GroupPadding - GroupLabelHeight,
            maxX.Value - minX.Value + GroupPadding * 2,
            maxY.Value - minY.Value + GroupPadding * 2 + GroupLabelHeight
        );
    }

    static void DrawGroup(SvgBuilder builder, ArchitectureGroup group,
        (double x, double y, double width, double height) bounds, string color, RenderOptions options)
    {
        var icon = group.Icon ?? "cloud";
        var borderColor = iconColors.GetValueOrDefault(icon, "#90A4AE");

        // Group background with dashed border
        builder.AddRect(bounds.x, bounds.y, bounds.width, bounds.height, rx: 8,
            fill: color, stroke: borderColor, strokeWidth: 2, style: "stroke-dasharray: 5,3");

        // Group label
        var label = group.Label ?? group.Id;
        builder.AddText(bounds.x + GroupPadding, bounds.y + GroupLabelHeight / 2 + GroupPadding / 2, label,
            anchor: "start", baseline: "middle",
            fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily,
            fontWeight: "bold", fill: "#333");
    }

    static void DrawService(SvgBuilder builder, ArchitectureService service, double x, double y, RenderOptions options)
    {
        var icon = service.Icon ?? "server";
        var color = iconColors.GetValueOrDefault(icon, "#90A4AE");

        // Background
        builder.AddRect(x, y, ServiceWidth, ServiceHeight, rx: 8,
            fill: "#FAFAFA", stroke: color, strokeWidth: 2);

        // Icon
        if (IconPaths.TryGetValue(icon, out var path))
        {
            var iconX = x + (ServiceWidth - IconSize) / 2;
            var iconY = y + 8;
            builder.BeginGroup(transform: $"translate({Fmt(iconX)},{Fmt(iconY)}) scale(0.64)");
            builder.AddPath(path, fill: color, stroke: "#333", strokeWidth: 1);
            builder.EndGroup();
        }

        // Label
        var label = service.Label ?? service.Id;
        builder.AddText(x + ServiceWidth / 2, y + ServiceHeight - 12, label,
            anchor: "middle", baseline: "middle",
            fontSize: $"{options.FontSize - 2}px", fontFamily: options.FontFamily,
            fill: "#333");
    }

    static void DrawJunction(SvgBuilder builder, ArchitectureJunction junction, double x, double y, RenderOptions options) =>
        builder.AddCircle(x, y, 8, fill: "#666", stroke: "#333", strokeWidth: 1);

    static void DrawEdge(SvgBuilder builder, (double x, double y) from, (double x, double y) to,
        ArchitectureEdge edge, RenderOptions options)
    {
        // Calculate edge start/end based on direction
        var fromOffset = GetDirectionOffset(edge.SourceSide);
        var toOffset = GetDirectionOffset(edge.TargetSide);

        var fromX = from.x + fromOffset.x * ServiceWidth / 2;
        var fromY = from.y + fromOffset.y * ServiceHeight / 2;
        var toX = to.x + toOffset.x * ServiceWidth / 2;
        var toY = to.y + toOffset.y * ServiceHeight / 2;

        // Draw line
        builder.AddLine(fromX, fromY, toX, toY, stroke: "#666", strokeWidth: 1.5);

        // Draw arrows
        if (edge.TargetArrow)
        {
            var angle = Math.Atan2(toY - fromY, toX - fromX);
            DrawArrow(builder, toX, toY, angle);
        }

        if (edge.SourceArrow)
        {
            var angle = Math.Atan2(fromY - toY, fromX - toX);
            DrawArrow(builder, fromX, fromY, angle);
        }
    }

    static (double x, double y) GetDirectionOffset(EdgeDirection dir) => dir switch
    {
        EdgeDirection.Left => (-1, 0),
        EdgeDirection.Right => (1, 0),
        EdgeDirection.Top => (0, -1),
        EdgeDirection.Bottom => (0, 1),
        _ => (0, 0)
    };

    static void DrawArrow(SvgBuilder builder, double x, double y, double angle)
    {
        var arrowSize = 8;
        var arrowAngle = Math.PI / 6;
        var ax1 = x - arrowSize * Math.Cos(angle - arrowAngle);
        var ay1 = y - arrowSize * Math.Sin(angle - arrowAngle);
        var ax2 = x - arrowSize * Math.Cos(angle + arrowAngle);
        var ay2 = y - arrowSize * Math.Sin(angle + arrowAngle);

        builder.AddPath($"M {Fmt(x)} {Fmt(y)} L {Fmt(ax1)} {Fmt(ay1)} L {Fmt(ax2)} {Fmt(ay2)} Z",
            fill: "#666", stroke: "none");
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
