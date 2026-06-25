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

        // Render relationships first (behind entities). Relationships sharing an entity pair are fanned
        // apart so their lines and labels sit side by side instead of stacking on top of each other.
        var parallelGroups = BuildParallelGroups(model.Relationships, options);
        foreach (var relationship in model.Relationships)
        {
            var parallel = parallelGroups.GetValueOrDefault(relationship, ParallelInfo.Single);
            RenderRelationship(builder, relationship, entitiesByName, options, parallel);
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

    static void RenderRelationship(SvgBuilder builder, Relationship rel, Dictionary<string, Entity> entitiesByName, RenderOptions options, ParallelInfo parallel)
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

        // A self-referencing relationship can't use a straight line between two distinct borders; draw a
        // loop off the entity's right side instead (otherwise the line degenerates to a point and the
        // label is clipped against the entity's top edge).
        if (fromEntity == toEntity)
        {
            RenderSelfRelationship(builder, rel, fromEntity, options);
            return;
        }

        var (startX, startY) = GetConnectionPoint(fromEntity, toEntity);
        var (endX, endY) = GetConnectionPoint(toEntity, fromEntity);

        // Fan this line aside — keeping its endpoints on the entity edges — when it shares an entity pair.
        (startX, startY, endX, endY) = ApplyParallelOffset(fromEntity, toEntity, startX, startY, endX, endY, parallel, options);

        var dashArray = rel.Identifying ? null : "5,5";

        // Draw line
        builder.AddLine(
            startX,
            startY,
            endX,
            endY,
            stroke: "#333",
            strokeWidth: 1,
            strokeDasharray: dashArray);

        // Draw cardinality markers
        DrawCardinalityMarker(builder, startX, startY, endX, endY, rel.FromCardinality);
        DrawCardinalityMarker(builder, endX, endY, startX, startY, rel.ToCardinality);

        // Draw label if present
        if (string.IsNullOrEmpty(rel.Label))
        {
            return;
        }

        var labelX = (startX + endX) / 2;
        var labelY = (startY + endY) / 2;

        // Background for the label, sized to the text (plus a small margin) so it masks only as much of the
        // relationship line as the text actually covers. An oversized box visibly chops the line — most of
        // all where the line crosses the label diagonally, since the box is axis-aligned.
        var labelFontSize = options.FontSize - 2;
        var labelWidth = MeasureText(rel.Label, labelFontSize) + 8;
        var labelHeight = labelFontSize + 4;
        builder.AddRect(
            labelX - labelWidth / 2,
            labelY - labelHeight / 2,
            labelWidth,
            labelHeight,
            fill: "#fff",
            stroke: "none");

        builder.AddText(
            labelX,
            labelY,
            rel.Label,
            anchor: "middle",
            baseline: "middle",
            fontSize: labelFontSize,
            fontFamily: options.FontFamily,
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
            builder.AddRect(labelX - labelWidth / 2, centerY - labelHeight / 2, labelWidth, labelHeight, fill: "#fff", stroke: "none");
            builder.AddText(
                labelX,
                centerY,
                rel.Label,
                anchor: "middle",
                baseline: "middle",
                fontSize: labelFontSize,
                fontFamily: options.FontFamily,
                fill: "#333");
        }
    }

    static (double x, double y) GetConnectionPoint(Entity from, Entity to)
    {
        var dx = to.Position.X - from.Position.X;
        var dy = to.Position.Y - from.Position.Y;

        if (Math.Abs(dx) > Math.Abs(dy))
        {
            if (dx > 0)
            {
                return (from.Position.X + from.Width / 2, from.Position.Y);
            }

            return (from.Position.X - from.Width / 2, from.Position.Y);
        }

        if (dy > 0)
        {
            return (from.Position.X, from.Position.Y + from.Height / 2);
        }

        return (from.Position.X, from.Position.Y - from.Height / 2);
    }

    // Group relationships by the (unordered) pair of entities they connect, so a set of parallel
    // relationships can be fanned apart. Self-relationships are excluded — they are drawn as loops.
    static Dictionary<Relationship, ParallelInfo> BuildParallelGroups(List<Relationship> relationships, RenderOptions options)
    {
        var groups = new Dictionary<string, List<Relationship>>(StringComparer.Ordinal);
        foreach (var relationship in relationships)
        {
            if (relationship.FromEntity == relationship.ToEntity)
            {
                continue;
            }

            var key = PairKey(relationship.FromEntity, relationship.ToEntity);
            if (!groups.TryGetValue(key, out var members))
            {
                members = [];
                groups[key] = members;
            }

            members.Add(relationship);
        }

        var result = new Dictionary<Relationship, ParallelInfo>();
        foreach (var members in groups.Values)
        {
            var maxLabelWidth = 0.0;
            foreach (var member in members)
            {
                if (!string.IsNullOrEmpty(member.Label))
                {
                    maxLabelWidth = Math.Max(maxLabelWidth, MeasureText(member.Label, options.FontSize - 2));
                }
            }

            for (var index = 0; index < members.Count; index++)
            {
                result[members[index]] = new(index, members.Count, maxLabelWidth);
            }
        }

        return result;
    }

    // Order-independent key for an entity pair, so A→B and B→A land in the same parallel group.
    static string PairKey(string first, string second) =>
        string.CompareOrdinal(first, second) <= 0
            ? $"{first} {second}"
            : $"{second} {first}";

    static (double startX, double startY, double endX, double endY) ApplyParallelOffset(
        Entity fromEntity,
        Entity toEntity,
        double startX,
        double startY,
        double endX,
        double endY,
        ParallelInfo parallel,
        RenderOptions options)
    {
        if (parallel.Count <= 1)
        {
            return (startX, startY, endX, endY);
        }

        // Mainly-stacked entities connect through their top/bottom edges, so the fan spreads along X to keep
        // both endpoints on those edges; side-by-side entities connect through left/right edges and spread
        // along Y. Offsetting along the shared edge (not perpendicular to the line) is what keeps each line
        // anchored to the border rather than sliding off a diagonal edge into — or away from — the box.
        var stacked = Math.Abs(toEntity.Position.Y - fromEntity.Position.Y) >=
                      Math.Abs(toEntity.Position.X - fromEntity.Position.X);

        // Stacked entities lay their labels side by side (space by label width); side-by-side entities stack
        // the labels (label height is enough).
        var spacing = stacked
            ? parallel.MaxLabelWidth + 12
            : options.FontSize - 2 + 10;

        // Centre the fan on the original line: e.g. two lines land at -spacing/2 and +spacing/2.
        var shift = (parallel.Index - (parallel.Count - 1) / 2.0) * spacing;

        // Never let the fan carry an endpoint past the shorter entity's edge.
        var limit = (stacked
            ? Math.Min(fromEntity.Width, toEntity.Width)
            : Math.Min(fromEntity.Height, toEntity.Height)) / 2 - 8;
        shift = Math.Clamp(shift, -limit, limit);

        return stacked
            ? (startX + shift, startY, endX + shift, endY)
            : (startX, startY + shift, endX, endY + shift);
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

    // One relationship's place within its set of parallel relationships (those joining the same entity pair):
    // its position in the fan, how many there are, and the widest label in the set (drives line spacing).
    readonly record struct ParallelInfo(int Index, int Count, double MaxLabelWidth)
    {
        public static ParallelInfo Single { get; } = new(0, 1, 0);
    }
}

// Internal graph model for layout