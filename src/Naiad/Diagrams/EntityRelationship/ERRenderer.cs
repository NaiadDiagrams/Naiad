namespace Naiad.Diagrams.EntityRelationship;

public class ERRenderer(ILayoutEngine? layoutEngine = null) :
    IDiagramRenderer<ERModel>
{
    readonly ILayoutEngine layoutEngine = layoutEngine ?? new DagreEngine();

    const double entityPadding = 10;
    const double lineHeight = 20;
    const double minEntityWidth = 120;
    const double attributeIndent = 10;
    const double commentGap = 10;

    // The gutter the PK/FK/UK indicator sits in, left of the attribute text.
    const double keyColumnWidth = 20;
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
            maxTextWidth = Math.Max(maxTextWidth, MeasureAttribute(attr, options));
        }

        // Must match where the attribute text is actually drawn, or the longest row overflows the box.
        var width = Math.Max(
            minEntityWidth,
            maxTextWidth + entityPadding * 2 + attributeIndent + keyColumnWidth);

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

            var attrX = x + entityPadding + attributeIndent + keyColumnWidth;
            builder.AddText(
                attrX,
                attrY + lineHeight / 2, attrText,
                anchor: "start",
                baseline: "middle",
                fontSize: options.FontSize,
                fontFamily: options.FontFamily);

            if (!string.IsNullOrEmpty(attr.Comment))
            {
                builder.AddText(
                    attrX + MeasureText(attrText, options.FontSize) + commentGap,
                    attrY + lineHeight / 2, attr.Comment,
                    anchor: "start",
                    baseline: "middle",
                    fontSize: options.FontSize - 2,
                    fontFamily: options.FontFamily,
                    fill: "#888");
            }

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

    // Crow's-foot marks, as distances out from the entity border along the edge.
    const double markerSpan = 10;      // perpendicular width of a bar and of the foot's mouth
    const double forkLength = 12;      // how far the foot's apex sits from the border
    const double nearBar = 7;
    const double farBar = 13;
    const double optionalCircleRadius = 4;
    const double optionalCircle = 15;  // the "zero" of |o, clear of the bar at nearBar
    const double barBeyondFork = 17;   // the "one" of |{, clear of the foot's apex
    const double circleBeyondFork = 18;

    /// <summary>
    /// Draws one end's cardinality. <paramref name="x"/>/<paramref name="y"/> is on the entity border and
    /// the angle to <paramref name="toX"/>/<paramref name="toY"/> points away along the edge. Crow's-foot
    /// notation reads outwards from the entity: the maximum-cardinality mark (the foot, or the bar of a
    /// "one") sits against the border and the minimum (the circle of a "zero", or the second bar) beyond
    /// it — so the foot opens onto the entity rather than pointing at it like an arrowhead.
    /// </summary>
    static void DrawCardinalityMarker(
        SvgBuilder builder,
        double x,
        double y,
        double toX,
        double toY,
        Cardinality cardinality)
    {
        var angle = Math.Atan2(toY - y, toX - x);

        switch (cardinality)
        {
            case Cardinality.ExactlyOne:
                // ||
                DrawBar(builder, x, y, angle, nearBar);
                DrawBar(builder, x, y, angle, farBar);
                break;

            case Cardinality.ZeroOrOne:
                // |o - bar against the entity, then the optional circle
                DrawBar(builder, x, y, angle, nearBar);
                DrawOptionalCircle(builder, x, y, angle, optionalCircle);
                break;

            case Cardinality.OneOrMore:
                // |{ - foot against the entity, then the bar
                DrawCrowFoot(builder, x, y, angle);
                DrawBar(builder, x, y, angle, barBeyondFork);
                break;

            case Cardinality.ZeroOrMore:
                // o{ - foot against the entity, then the optional circle
                DrawCrowFoot(builder, x, y, angle);
                DrawOptionalCircle(builder, x, y, angle, circleBeyondFork);
                break;
        }
    }

    static void DrawBar(SvgBuilder builder, double x, double y, double angle, double distance)
    {
        var cx = x + distance * Math.Cos(angle);
        var cy = y + distance * Math.Sin(angle);
        var perpX = Math.Cos(angle + Math.PI / 2);
        var perpY = Math.Sin(angle + Math.PI / 2);

        builder.AddLine(
            cx - perpX * markerSpan / 2,
            cy - perpY * markerSpan / 2,
            cx + perpX * markerSpan / 2,
            cy + perpY * markerSpan / 2,
            stroke: "#333",
            strokeWidth: 1);
    }

    static void DrawOptionalCircle(SvgBuilder builder, double x, double y, double angle, double distance) =>
        builder.AddCircle(
            x + distance * Math.Cos(angle),
            y + distance * Math.Sin(angle),
            optionalCircleRadius,
            fill: "#fff",
            stroke: "#333",
            strokeWidth: 1);

    static void DrawCrowFoot(SvgBuilder builder, double x, double y, double angle)
    {
        // The three prongs converge at an apex away from the entity and fan out to meet the border.
        var apexX = x + forkLength * Math.Cos(angle);
        var apexY = y + forkLength * Math.Sin(angle);
        var perpX = Math.Cos(angle + Math.PI / 2);
        var perpY = Math.Sin(angle + Math.PI / 2);

        builder.AddLine(apexX, apexY, x, y, stroke: "#333", strokeWidth: 1);
        builder.AddLine(
            apexX,
            apexY,
            x + perpX * markerSpan / 2,
            y + perpY * markerSpan / 2,
            stroke: "#333",
            strokeWidth: 1);
        builder.AddLine(
            apexX,
            apexY,
            x - perpX * markerSpan / 2,
            y - perpY * markerSpan / 2,
            stroke: "#333",
            strokeWidth: 1);
    }

    static string FormatAttribute(EntityAttribute attr) => $"{attr.Type} {attr.Name}";

    /// <summary>
    /// Width of an attribute row. The comment is a column of its own rather than part of the declaration
    /// text — the quotes around it in the source are delimiters, so reproducing them would be wrong, but
    /// running the words straight on from the attribute name would be unreadable.
    /// </summary>
    static double MeasureAttribute(EntityAttribute attr, RenderOptions options)
    {
        var width = MeasureText(FormatAttribute(attr), options.FontSize);
        if (!string.IsNullOrEmpty(attr.Comment))
        {
            width += commentGap + MeasureText(attr.Comment, options.FontSize - 2);
        }

        return width;
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
