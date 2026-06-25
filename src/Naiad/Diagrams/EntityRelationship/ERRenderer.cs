namespace Naiad.Diagrams.EntityRelationship;

public class ERRenderer(ILayoutEngine? layoutEngine = null) :
    IDiagramRenderer<ERModel>
{
    readonly ILayoutEngine layoutEngine = layoutEngine ?? new DagreEngine();

    const double entityPadding = 10;
    const double lineHeight = 20;
    const double minEntityWidth = 120;
    const double attributeIndent = 10;
    const double headerHeight = 30;

    public SvgDocument Render(ERModel model, RenderOptions options)
    {
        // Calculate entity sizes and convert to graph model
        var graphModel = ConvertToGraphModel(model, options);

        // Run layout
        var layoutOptions = new LayoutOptions
        {
            Direction = model.Direction,
            NodeSeparation = 80,
            RankSeparation = 100
        };
        var layoutResult = layoutEngine.BuildLayout(graphModel, layoutOptions);

        // Copy positions back to entities
        CopyPositionsToModel(model, graphModel);

        // Build SVG
        var builder = new SvgBuilder();
        builder.Size(layoutResult.Width, layoutResult.Height);
        builder.Padding(options.Padding);

        // Index entities by name so relationship endpoint lookups are O(1), not O(R·E) via List.Find.
        var entitiesByName = new Dictionary<string, Entity>(StringComparer.Ordinal);
        foreach (var entity in model.Entities)
        {
            entitiesByName[entity.Name] = entity;
        }

        // Relationships render behind the entities. Each is paired with the edge Dagre routed for it (edges
        // are built in relationship order), so the curved path, the separation of parallel relationships and
        // the label gap all come straight from the shared layout rather than being re-derived here.
        for (var index = 0; index < model.Relationships.Count; index++)
        {
            RenderRelationship(builder, model.Relationships[index], graphModel.Edges[index], entitiesByName, options);
        }

        // Render entities
        foreach (var entity in model.Entities)
        {
            RenderEntity(builder, entity, options);
        }

        return builder.Build();
    }

    static GraphDiagramBase ConvertToGraphModel(ERModel model, RenderOptions options)
    {
        var graph = new ERLayoutGraph
        {
            Direction = model.Direction
        };

        foreach (var entity in model.Entities)
        {
            var (width, height) = CalculateEntitySize(entity, options);
            entity.Width = width;
            entity.Height = height;

            var node = new Node
            {
                Id = entity.Name,
                Label = entity.Name,
                Width = width,
                Height = height
            };
            graph.AddNode(node);
        }

        foreach (var rel in model.Relationships)
        {
            var edge = new Edge
            {
                SourceId = rel.FromEntity,
                TargetId = rel.ToEntity,
                Label = rel.Label
            };

            // Reserve the label's footprint so the layout keeps a gap clear for it along the routed path.
            // Self-relationships are drawn as manual loops (their routed path is ignored), so they get none.
            if (!string.IsNullOrEmpty(rel.Label) && rel.FromEntity != rel.ToEntity)
            {
                edge.LabelWidth = MeasureText(rel.Label, options.FontSize - 2) + 8;
                edge.LabelHeight = options.FontSize - 2 + 4;
            }

            graph.AddEdge(edge);
        }

        return graph;
    }

    static (double width, double height) CalculateEntitySize(Entity entity, RenderOptions options)
    {
        // Calculate width based on longest text
        var maxTextWidth = MeasureText(entity.Name, options.FontSize, true);

        foreach (var attr in entity.Attributes)
        {
            var attrText = FormatAttribute(attr);
            maxTextWidth = Math.Max(maxTextWidth, MeasureText(attrText, options.FontSize));
        }

        var width = Math.Max(minEntityWidth, maxTextWidth + entityPadding * 2 + attributeIndent);

        // Calculate height
        var height = headerHeight; // Entity name header
        if (entity.Attributes.Count > 0)
        {
            height += entity.Attributes.Count * lineHeight + entityPadding;
        }

        return (width, height);
    }

    static void CopyPositionsToModel(ERModel model, GraphDiagramBase graph)
    {
        foreach (var entity in model.Entities)
        {
            var node = graph.GetNode(entity.Name);
            if (node != null)
            {
                entity.Position = node.Position;
            }
        }
    }

    static void RenderEntity(SvgBuilder builder, Entity entity, RenderOptions options)
    {
        var x = entity.Position.X - entity.Width / 2;
        var y = entity.Position.Y - entity.Height / 2;
        var centerX = entity.Position.X;

        // Entity box
        builder.AddRect(
            x,
            y,
            entity.Width,
            entity.Height,
            rx: 0,
            fill: "#ECECFF",
            stroke: "#9370DB",
            strokeWidth: 2);

        // Entity name header
        builder.AddRect(
            x,
            y,
            entity.Width,
            headerHeight,
            fill: "#9370DB",
            stroke: "#9370DB",
            strokeWidth: 1);

        builder.AddText(
            centerX,
            y + headerHeight / 2,
            entity.Name,
            anchor: "middle",
            baseline: "middle",
            fontSize: options.FontSize,
            fontFamily: options.FontFamily,
            fontWeight: "bold",
            fill: "#fff");

        // Separator line
        if (entity.Attributes.Count > 0)
        {
            builder.AddLine(
                x,
                y + headerHeight,
                x + entity.Width,
                y + headerHeight,
                stroke: "#9370DB",
                strokeWidth: 1);
        }

        // Attributes
        var attrY = y + headerHeight + entityPadding;
        foreach (var attr in entity.Attributes)
        {
            var attrText = FormatAttribute(attr);
            var keyIndicator = GetKeyIndicator(attr.KeyType);

            if (!string.IsNullOrEmpty(keyIndicator))
            {
                builder.AddText(
                    x + entityPadding,
                    attrY + lineHeight / 2, keyIndicator,
                    anchor: "start",
                    baseline: "middle",
                    fontSize: options.FontSize - 2,
                    fontFamily: options.FontFamily,
                    fill: "#666");
            }

            builder.AddText(
                x + entityPadding + attributeIndent + 20,
                attrY + lineHeight / 2, attrText,
                anchor: "start",
                baseline: "middle",
                fontSize: options.FontSize,
                fontFamily: options.FontFamily);

            attrY += lineHeight;
        }
    }

    static void RenderRelationship(SvgBuilder builder, Relationship rel, Edge edge, Dictionary<string, Entity> entitiesByName, RenderOptions options)
    {
        var fromEntity = entitiesByName.GetValueOrDefault(rel.FromEntity);
        if (fromEntity == null)
        {
            return;
        }

        var toEntity = entitiesByName.GetValueOrDefault(rel.ToEntity);
        if (toEntity == null)
        {
            return;
        }

        // A self-referencing relationship has no meaningful routed path between two distinct borders; draw a
        // loop off the entity's right side instead (otherwise the line degenerates to a point and the label
        // is clipped against the entity's top edge).
        if (fromEntity == toEntity)
        {
            RenderSelfRelationship(builder, rel, fromEntity, options);
            return;
        }

        var points = edge.Points;
        if (points.Count < 2)
        {
            return;
        }

        var dashArray = rel.Identifying ? null : "5,5";

        // The Dagre-routed, B-spline-smoothed path. Parallel relationships route as separate curves and the
        // layout has already opened a gap for the label, so no manual fan-out or line masking is needed.
        builder.AddPath(
            EdgePath.Build(points),
            fill: "none",
            stroke: "#333",
            strokeWidth: 1,
            strokeDasharray: dashArray);

        // Cardinality markers sit at each end of the routed path, oriented by the adjacent waypoint so they
        // follow the curve's tangent into the entity. Source end carries FromCardinality, target end ToCardinality.
        var start = points[0];
        var end = points[^1];
        DrawCardinalityMarker(builder, start.X, start.Y, points[1].X, points[1].Y, rel.FromCardinality);
        DrawCardinalityMarker(builder, end.X, end.Y, points[^2].X, points[^2].Y, rel.ToCardinality);

        if (string.IsNullOrEmpty(rel.Label))
        {
            return;
        }

        var labelPosition = edge.LabelPosition;

        // Background sized to the text (plus a small margin) so it masks only as much of the path as the
        // label covers; the layout reserved a matching gap here when it routed the edge.
        var labelFontSize = options.FontSize - 2;
        var labelWidth = MeasureText(rel.Label, labelFontSize) + 8;
        var labelHeight = labelFontSize + 4;
        builder.AddEdgeLabel(
            labelPosition.X,
            labelPosition.Y,
            labelWidth,
            labelHeight,
            rel.Label,
            labelFontSize,
            options.FontFamily,
            fill: "#333");
    }

    static void RenderSelfRelationship(SvgBuilder builder, Relationship rel, Entity entity, RenderOptions options)
    {
        var right = entity.Position.X + entity.Width / 2;
        var centerY = entity.Position.Y;
        const double verticalOffset = 14;
        const double extent = 30;
        var topY = centerY - verticalOffset;
        var bottomY = centerY + verticalOffset;
        var outX = right + extent;
        var dashArray = rel.Identifying ? null : "5,5";

        // Loop out from the top of the right edge, around, and back to the bottom of the right edge.
        builder.AddPath(
            FormattableString.Invariant($"M{right},{topY} L{outX},{topY} L{outX},{bottomY} L{right},{bottomY}"),
            fill: "none",
            stroke: "#333",
            strokeWidth: 1,
            strokeDasharray: dashArray);

        DrawCardinalityMarker(builder, right, topY, outX, topY, rel.FromCardinality);
        DrawCardinalityMarker(builder, right, bottomY, outX, bottomY, rel.ToCardinality);

        if (!string.IsNullOrEmpty(rel.Label))
        {
            var labelFontSize = options.FontSize - 2;
            var labelWidth = MeasureText(rel.Label, labelFontSize) + 8;
            var labelHeight = labelFontSize + 4;
            var labelX = outX + labelWidth / 2 + 4;
            builder.AddEdgeLabel(
                labelX,
                centerY,
                labelWidth,
                labelHeight,
                rel.Label,
                labelFontSize,
                options.FontFamily,
                fill: "#333");
        }
    }

    static void DrawCardinalityMarker(
        SvgBuilder builder,
        double x,
        double y,
        double toX,
        double toY,
        Cardinality cardinality)
    {
        var angle = Math.Atan2(toY - y, toX - x);
        const double markerDistance = 15.0;
        const double perpDistance = 8.0;

        // Position for the marker (offset from the entity)
        var mx = x + markerDistance * Math.Cos(angle);
        var my = y + markerDistance * Math.Sin(angle);

        // Perpendicular direction
        var perpX = Math.Cos(angle + Math.PI / 2);
        var perpY = Math.Sin(angle + Math.PI / 2);

        switch (cardinality)
        {
            case Cardinality.ExactlyOne:
                // Two vertical lines ||
                DrawLine(builder, mx, my, perpX, perpY, perpDistance);
                var mx2 = mx + 5 * Math.Cos(angle);
                var my2 = my + 5 * Math.Sin(angle);
                DrawLine(builder, mx2, my2, perpX, perpY, perpDistance);
                break;

            case Cardinality.ZeroOrOne:
                // Circle and line o|
                builder.AddCircle(mx, my, 4, fill: "#fff", stroke: "#333", strokeWidth: 1);
                var lineX = mx + 8 * Math.Cos(angle);
                var lineY = my + 8 * Math.Sin(angle);
                DrawLine(builder, lineX, lineY, perpX, perpY, perpDistance);
                break;

            case Cardinality.OneOrMore:
                // Three-pronged crow's foot with line |{
                DrawLine(builder, mx, my, perpX, perpY, perpDistance);
                DrawCrowFoot(builder, mx + 5 * Math.Cos(angle), my + 5 * Math.Sin(angle),
                    angle, perpDistance);
                break;

            case Cardinality.ZeroOrMore:
                // Circle and crow's foot o{
                builder.AddCircle(mx, my, 4, fill: "#fff", stroke: "#333", strokeWidth: 1);
                DrawCrowFoot(builder, mx + 8 * Math.Cos(angle), my + 8 * Math.Sin(angle),
                    angle, perpDistance);
                break;
        }
    }

    static void DrawLine(SvgBuilder builder, double x, double y, double perpX, double perpY, double length) =>
        builder.AddLine(
            x - perpX * length / 2,
            y - perpY * length / 2,
            x + perpX * length / 2,
            y + perpY * length / 2,
            stroke: "#333",
            strokeWidth: 1);

    static void DrawCrowFoot(SvgBuilder builder, double x, double y, double angle, double spread)
    {
        // Draw three lines from center point spreading outward
        var tipX = x + 8 * Math.Cos(angle);
        var tipY = y + 8 * Math.Sin(angle);

        // Center line
        builder.AddLine(x, y, tipX, tipY, stroke: "#333", strokeWidth: 1);

        // Upper line
        var perpX = Math.Cos(angle + Math.PI / 2);
        var perpY = Math.Sin(angle + Math.PI / 2);
        builder.AddLine(
            x,
            y,
            tipX + perpX * spread / 2,
            tipY + perpY * spread / 2,
            stroke: "#333",
            strokeWidth: 1);

        // Lower line
        builder.AddLine(
            x,
            y,
            tipX - perpX * spread / 2,
            tipY - perpY * spread / 2,
            stroke: "#333",
            strokeWidth: 1);
    }

    static string FormatAttribute(EntityAttribute attr)
    {
        var result = $"{attr.Type} {attr.Name}";
        if (!string.IsNullOrEmpty(attr.Comment))
        {
            result += $" \"{attr.Comment}\"";
        }

        return result;
    }

    static string GetKeyIndicator(AttributeKeyType keyType) =>
        keyType switch
        {
            AttributeKeyType.PrimaryKey => "PK",
            AttributeKeyType.ForeignKey => "FK",
            AttributeKeyType.UniqueKey => "UK",
            _ => ""
        };

    static double MeasureText(string text, double fontSize, bool bold = false)
    {
        var factor = bold ? 0.65 : 0.55;
        return text.Length * fontSize * factor;
    }
}
