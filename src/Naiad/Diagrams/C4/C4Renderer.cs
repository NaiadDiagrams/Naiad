namespace Naiad.Diagrams.C4;

public class C4Renderer(ILayoutEngine? layoutEngine = null) : IDiagramRenderer<C4Model>
{
    readonly ILayoutEngine layoutEngine = layoutEngine ?? new DagreLayoutEngine();

    const double ElementWidth = 160;
    const double ElementHeight = 100;
    const double PersonHeight = 120;
    const double TitleHeight = 50;
    const double BoundaryPadding = 15;
    const double BoundaryTitleHeight = 40;

    const string PersonColor = "#08427B";
    const string PersonExtColor = "#999999";
    const string SystemColor = "#1168BD";
    const string SystemDbColor = "#1168BD";
    const string SystemExtColor = "#999999";
    const string ContainerColor = "#438DD5";
    const string ContainerDbColor = "#438DD5";
    const string ComponentColor = "#85BBF0";
    const string BoundaryStroke = "#444444";
    const string BoundaryFill = "#FFFFFF";

    // Boundary layout state (recursive composite layout). Keyed by boundary id;
    // the top-level container uses the empty-string key.
    readonly Dictionary<string, ContainerLayout> containerLayouts = new();
    readonly Dictionary<string, (double w, double h)> boundarySizes = new();
    readonly Dictionary<string, (double x, double y, double w, double h)> elementAbs = new();
    readonly Dictionary<string, (double x, double y, double w, double h)> boundaryAbs = new();
    readonly Dictionary<string, (double x, double y)> containerOriginAbs = new();
    Dictionary<string, C4Element> elementsById = new();
    Dictionary<string, C4Boundary> boundariesById = new();

    public SvgDocument Render(C4Model model, RenderOptions options)
    {
        if (model.Elements.Count == 0 && model.Boundaries.Count == 0)
        {
            var emptyBuilder = new SvgBuilder().Size(200, 100);
            emptyBuilder.AddText(
                100,
                50,
                "Empty C4 diagram",
                anchor: "middle",
                baseline: "middle",
                fontSize: options.FontSize,
                fontFamily: options.FontFamily);
            return emptyBuilder.Build();
        }

        // Both paths place related elements adjacently with the shared
        // Sugiyama/Dagre engine. Boundary diagrams additionally lay out each
        // boundary's contents in isolation and treat the boundary as a single
        // composite node in its parent container.
        if (model.Boundaries.Count == 0)
        {
            return RenderWithLayoutEngine(model, options);
        }

        return RenderWithBoundaries(model, options);
    }

    /// <summary>
    /// Layout-engine path for boundary-free diagrams: builds a graph from the
    /// elements and relationships, runs the shared Sugiyama/Dagre engine for
    /// placement and edge routing, then draws C4 shapes at the computed
    /// positions with edges following the routed polylines.
    /// </summary>
    SvgDocument RenderWithLayoutEngine(C4Model model, RenderOptions options)
    {
        var graph = new C4LayoutGraph();

        foreach (var element in model.Elements)
        {
            graph.AddNode(
                new()
                {
                    Id = element.Id,
                    Label = element.Label,
                    Width = ElementWidth,
                    Height = element.Type == C4ElementType.Person ? PersonHeight : ElementHeight
                });
        }

        var edgePairs = new List<(Edge edge, C4Relationship rel)>();
        foreach (var rel in model.Relationships)
        {
            // Skip relationships that reference unknown elements.
            if (graph.GetNode(rel.From) is null ||
                graph.GetNode(rel.To) is null)
            {
                continue;
            }

            var edge = new Edge
            {
                SourceId = rel.From,
                TargetId = rel.To,
                Label = rel.Label,
                LineStyle = EdgeStyle.Dotted
            };
            graph.AddEdge(edge);
            edgePairs.Add((edge, rel));
        }

        var layoutOptions = new LayoutOptions
        {
            Direction = Direction.TopToBottom,
            NodeSeparation = 60,
            RankSeparation = 90
        };
        var layoutResult = layoutEngine.Layout(graph, layoutOptions);

        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : TitleHeight;

        // Ensure the canvas is wide enough for the title, which can be wider
        // than a narrow (e.g. single-column) diagram body.
        var titleWidth = string.IsNullOrEmpty(model.Title)
            ? 0
            : model.Title.Length * (options.FontSize + 6) * 0.6 + 20;
        var contentWidth = Math.Max(layoutResult.Width, titleWidth);
        var contentHeight = layoutResult.Height + titleOffset;

        var builder = new SvgBuilder()
            .Size(contentWidth, contentHeight)
            .Padding(options.Padding);

        if (!string.IsNullOrEmpty(model.Title))
        {
            builder.AddText(
                contentWidth / 2,
                TitleHeight / 2,
                model.Title,
                anchor: "middle",
                baseline: "middle",
                fontSize: options.FontSize + 6,
                fontFamily: options.FontFamily,
                fontWeight: "bold");
        }

        // Center the diagram body horizontally and offset it below the title.
        var bodyOffsetX = (contentWidth - layoutResult.Width) / 2;
        builder.BeginGroup(transform: string.Create(
            CultureInfo.InvariantCulture, $"translate({bodyOffsetX:0.##},{titleOffset:0.##})"));

        // Edge lines first so element boxes sit on top of them.
        foreach (var (edge, rel) in edgePairs)
        {
            DrawRoutedPolyline(builder, edge.Points, rel.Direction == C4RelationshipDirection.Back);
        }

        // Element boxes.
        foreach (var element in model.Elements)
        {
            var node = graph.GetNode(element.Id);
            if (node is null)
            {
                continue;
            }

            var h = element.Type == C4ElementType.Person ? PersonHeight : ElementHeight;
            DrawElement(builder, element, node.Position.X - ElementWidth / 2, node.Position.Y - h / 2, options);
        }

        // Edge labels last so their chips stay legible on top of everything.
        foreach (var (edge, rel) in edgePairs)
        {
            if (!string.IsNullOrEmpty(rel.Label))
            {
                DrawLabelChip(builder, edge.LabelPosition.X, edge.LabelPosition.Y, rel.Label, rel.Technology, options);
            }
        }

        builder.EndGroup();

        return builder.Build();
    }

    /// <summary>
    /// Layout path for diagrams with boundaries. Each boundary's contents are
    /// laid out in isolation (recursively), the boundary is treated as a single
    /// composite node in its parent, and edges are drawn leaf-to-leaf once every
    /// element has an absolute position.
    /// </summary>
    SvgDocument RenderWithBoundaries(C4Model model, RenderOptions options)
    {
        containerLayouts.Clear();
        boundarySizes.Clear();
        elementAbs.Clear();
        boundaryAbs.Clear();
        containerOriginAbs.Clear();
        elementsById = model.Elements.ToDictionary(_ => _.Id);
        boundariesById = model.Boundaries.ToDictionary(_ => _.Id);

        // Pass 1: lay out each container, children before parents.
        var topLayout = LayoutContainer(model, null);
        containerLayouts[""] = topLayout;

        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : TitleHeight;
        var titleWidth = string.IsNullOrEmpty(model.Title)
            ? 0
            : model.Title.Length * (options.FontSize + 6) * 0.6 + 20;
        var contentWidth = Math.Max(topLayout.ContentWidth, titleWidth);
        var contentHeight = topLayout.ContentHeight + titleOffset;

        // Pass 2: assign absolute positions, parents before children.
        var bodyOffsetX = (contentWidth - topLayout.ContentWidth) / 2;
        PlaceContainer(null, bodyOffsetX, titleOffset);

        var builder = new SvgBuilder()
            .Size(contentWidth, contentHeight)
            .Padding(options.Padding);

        if (!string.IsNullOrEmpty(model.Title))
        {
            builder.AddText(
                contentWidth / 2,
                TitleHeight / 2,
                model.Title,
                anchor: "middle",
                baseline: "middle",
                fontSize: options.FontSize + 6,
                fontFamily: options.FontFamily,
                fontWeight: "bold");
        }

        // Draw order: boundary boxes (outermost first) so their fills don't
        // cover nested content, then edge lines, then element boxes, then edge
        // label chips on top.
        foreach (var boundary in model.Boundaries.OrderBy(BoundaryDepth))
        {
            if (boundaryAbs.TryGetValue(boundary.Id, out var b))
            {
                DrawBoundaryBox(builder, boundary, b.x, b.y, b.w, b.h, options);
            }
        }

        var labels = new List<(double x, double y, string label, string? technology)>();
        foreach (var rel in model.Relationships)
        {
            if (!elementAbs.TryGetValue(rel.From, out var from) ||
                !elementAbs.TryGetValue(rel.To, out var to))
            {
                continue;
            }

            var reversed = rel.Direction == C4RelationshipDirection.Back;

            // Use the engine-routed polyline when both ends sit in the same
            // container (so a skipping edge routes around its siblings);
            // otherwise fall back to a straight, border-trimmed line.
            var (mx, my) = TryGetRoutedPolyline(rel, out var route)
                ? DrawRoutedPolyline(builder, route, reversed)
                : reversed
                    ? DrawRelationshipLine(builder, to, from)
                    : DrawRelationshipLine(builder, from, to);

            if (!string.IsNullOrEmpty(rel.Label))
            {
                labels.Add((mx, my, rel.Label, rel.Technology));
            }
        }

        foreach (var element in model.Elements)
        {
            if (elementAbs.TryGetValue(element.Id, out var e))
            {
                var h = element.Type == C4ElementType.Person ? PersonHeight : ElementHeight;
                DrawElement(builder, element, e.x - ElementWidth / 2, e.y - h / 2, options);
            }
        }

        foreach (var (x, y, label, technology) in labels)
        {
            DrawLabelChip(builder, x, y, label, technology, options);
        }

        return builder.Build();
    }

    /// <summary>
    /// Tries to build the absolute engine-routed polyline for a relationship
    /// whose endpoints share a container (so it can route around siblings).
    /// </summary>
    bool TryGetRoutedPolyline(C4Relationship rel, out List<Position> absolute)
    {
        absolute = [];
        if (!elementsById.TryGetValue(rel.From, out var from) ||
            !elementsById.TryGetValue(rel.To, out var to) ||
            from.BoundaryId != to.BoundaryId)
        {
            return false;
        }

        var key = from.BoundaryId ?? "";
        if (!containerLayouts.TryGetValue(key, out var layout) ||
            !containerOriginAbs.TryGetValue(key, out var origin) ||
            !layout.EdgeRoutes.TryGetValue((rel.From, rel.To), out var points) ||
            points.Count < 2)
        {
            return false;
        }

        absolute = points.Select(_ => new Position(_.X + origin.x, _.Y + origin.y)).ToList();
        return true;
    }

    /// <summary>
    /// Lays out a single container (the top level when <paramref name="boundaryId"/>
    /// is null, otherwise one boundary) using the shared engine, treating child
    /// boundaries as composite nodes. Returns the content size and each direct
    /// member's center relative to the content's top-left.
    /// </summary>
    ContainerLayout LayoutContainer(C4Model model, string? boundaryId)
    {
        var directElements = model.Elements.Where(_ => _.BoundaryId == boundaryId).ToList();
        var childBoundaries = model.Boundaries.Where(_ => _.ParentBoundaryId == boundaryId).ToList();

        // Lay out child boundaries first so their composite sizes are known.
        foreach (var child in childBoundaries)
        {
            var childLayout = LayoutContainer(model, child.Id);
            containerLayouts[child.Id] = childLayout;
            boundarySizes[child.Id] = (
                childLayout.ContentWidth + BoundaryPadding * 2,
                childLayout.ContentHeight + BoundaryPadding * 2 + BoundaryTitleHeight);
        }

        var graph = new C4LayoutGraph();
        foreach (var element in directElements)
        {
            graph.AddNode(
                new()
                {
                    Id = element.Id,
                    Width = ElementWidth,
                    Height = element.Type == C4ElementType.Person ? PersonHeight : ElementHeight
                });
        }

        foreach (var child in childBoundaries)
        {
            var (w, h) = boundarySizes[child.Id];
            graph.AddNode(new() { Id = child.Id, Width = w, Height = h });
        }

        // Add an edge between two members only at the level where they first
        // become distinct direct members (their lowest common container).
        foreach (var rel in model.Relationships)
        {
            var from = DirectRepresentative(rel.From, boundaryId);
            var to = DirectRepresentative(rel.To, boundaryId);
            if (from is not null &&
                to is not null &&
                from != to &&
                graph.GetNode(from) is not null &&
                graph.GetNode(to) is not null)
            {
                graph.AddEdge(new() { SourceId = from, TargetId = to });
            }
        }

        var layout = new ContainerLayout();
        if (graph.Nodes.Count == 0)
        {
            layout.ContentWidth = ElementWidth;
            layout.ContentHeight = ElementHeight;
            return layout;
        }

        // Actors flow top-to-bottom at the top level; a boundary's contents are
        // laid out left-to-right so an edge leaving the boundary downward does
        // not pass through a sibling box stacked below it.
        var layoutOptions = new LayoutOptions
        {
            Direction = boundaryId is null ? Direction.TopToBottom : Direction.LeftToRight,
            NodeSeparation = 60,
            RankSeparation = 90
        };
        layoutEngine.Layout(graph, layoutOptions);

        var minX = double.MaxValue;
        var minY = double.MaxValue;
        var maxX = double.MinValue;
        var maxY = double.MinValue;
        foreach (var node in graph.Nodes)
        {
            minX = Math.Min(minX, node.Position.X - node.Width / 2);
            minY = Math.Min(minY, node.Position.Y - node.Height / 2);
            maxX = Math.Max(maxX, node.Position.X + node.Width / 2);
            maxY = Math.Max(maxY, node.Position.Y + node.Height / 2);
        }

        foreach (var node in graph.Nodes)
        {
            layout.MemberCenters[node.Id] = (node.Position.X - minX, node.Position.Y - minY);
        }

        // Keep the engine-routed polyline for each edge (content-relative) so a
        // relationship that skips a sibling routes around it instead of crossing.
        foreach (var edge in graph.Edges)
        {
            if (edge.Points.Count < 2)
            {
                continue;
            }

            layout.EdgeRoutes[(edge.SourceId, edge.TargetId)] =
                edge.Points.Select(_ => new Position(_.X - minX, _.Y - minY)).ToList();
        }

        layout.ContentWidth = maxX - minX;
        layout.ContentHeight = maxY - minY;
        return layout;
    }

    /// <summary>
    /// Returns the id of the direct child of <paramref name="containerId"/> (an
    /// element or a child boundary) that contains <paramref name="elementId"/>,
    /// or null if the element is not within that container.
    /// </summary>
    string? DirectRepresentative(string elementId, string? containerId)
    {
        if (!elementsById.TryGetValue(elementId, out var element))
        {
            return null;
        }

        // The element sits directly inside this container.
        if (element.BoundaryId == containerId)
        {
            return elementId;
        }

        // Otherwise find the boundary ancestor that is a direct child of the
        // container; that boundary is the element's representative here.
        var current = element.BoundaryId;
        while (current is not null)
        {
            if (!boundariesById.TryGetValue(current, out var boundary))
            {
                return null;
            }

            if (boundary.ParentBoundaryId == containerId)
            {
                return current;
            }

            current = boundary.ParentBoundaryId;
        }

        return null;
    }

    /// <summary>
    /// Recursively assigns absolute positions. <paramref name="contentOriginX"/>
    /// / <paramref name="contentOriginY"/> are the absolute top-left of the
    /// container's content area; each member's center is offset from there.
    /// </summary>
    void PlaceContainer(string? boundaryId, double contentOriginX, double contentOriginY)
    {
        if (!containerLayouts.TryGetValue(boundaryId ?? "", out var layout))
        {
            return;
        }

        containerOriginAbs[boundaryId ?? ""] = (contentOriginX, contentOriginY);

        foreach (var (memberId, center) in layout.MemberCenters)
        {
            var centerX = contentOriginX + center.X;
            var centerY = contentOriginY + center.Y;

            if (boundariesById.ContainsKey(memberId))
            {
                var (w, h) = boundarySizes[memberId];
                var topLeftX = centerX - w / 2;
                var topLeftY = centerY - h / 2;
                boundaryAbs[memberId] = (topLeftX, topLeftY, w, h);
                PlaceContainer(
                    memberId,
                    topLeftX + BoundaryPadding,
                    topLeftY + BoundaryTitleHeight + BoundaryPadding);
            }
            else if (elementsById.TryGetValue(memberId, out var element))
            {
                var h = element.Type == C4ElementType.Person ? PersonHeight : ElementHeight;
                elementAbs[memberId] = (centerX, centerY, ElementWidth, h);
            }
        }
    }

    /// <summary>Nesting depth of a boundary (0 for a top-level boundary).</summary>
    int BoundaryDepth(C4Boundary boundary)
    {
        var depth = 0;
        var current = boundary.ParentBoundaryId;
        while (current is not null && boundariesById.TryGetValue(current, out var parent))
        {
            depth++;
            current = parent.ParentBoundaryId;
        }

        return depth;
    }

    /// <summary>
    /// Draws a dashed polyline through the routed layout points with a manual
    /// arrowhead, and returns the point where its label chip should sit. The
    /// arrowhead is placed at the target end, or at the source end when
    /// <paramref name="reversed"/> is set (a "back" relationship).
    /// </summary>
    static (double x, double y) DrawRoutedPolyline(
        SvgBuilder builder,
        IReadOnlyList<Position> points,
        bool reversed)
    {
        if (points.Count < 2)
        {
            return points.Count == 1 ? (points[0].X, points[0].Y - 8) : (0, 0);
        }

        var path = new StringBuilder();
        path.Append(CultureInfo.InvariantCulture, $"M {points[0].X:0.##} {points[0].Y:0.##}");
        for (var i = 1; i < points.Count; i++)
        {
            path.Append(CultureInfo.InvariantCulture, $" L {points[i].X:0.##} {points[i].Y:0.##}");
        }

        builder.AddPath(
            path.ToString(),
            fill: "none",
            stroke: "#666",
            strokeWidth: 1.5,
            strokeDasharray: "5,5");

        var tip = reversed ? points[0] : points[^1];
        var prev = reversed ? points[1] : points[^2];
        var angle = Math.Atan2(tip.Y - prev.Y, tip.X - prev.X);
        const int arrowSize = 8;
        const double arrowAngle = Math.PI / 6;
        var ax1 = tip.X - arrowSize * Math.Cos(angle - arrowAngle);
        var ay1 = tip.Y - arrowSize * Math.Sin(angle - arrowAngle);
        var ax2 = tip.X - arrowSize * Math.Cos(angle + arrowAngle);
        var ay2 = tip.Y - arrowSize * Math.Sin(angle + arrowAngle);

        builder.AddPath(
            string.Create(CultureInfo.InvariantCulture, $"M {tip.X:0.##} {tip.Y:0.##} L {ax1:0.##} {ay1:0.##} L {ax2:0.##} {ay2:0.##} Z"),
            fill: "#666",
            stroke: "none");

        // Label sits at the polyline midpoint.
        var mid = points.Count / 2;
        if (points.Count % 2 == 0)
        {
            return ((points[mid - 1].X + points[mid].X) / 2, (points[mid - 1].Y + points[mid].Y) / 2 - 8);
        }

        return (points[mid].X, points[mid].Y - 8);
    }

    /// <summary>
    /// Draws a relationship as a dashed line with an arrowhead, trimmed to the
    /// box edges, and returns the point where its label chip should sit.
    /// </summary>
    static (double x, double y) DrawRelationshipLine(
        SvgBuilder builder,
        (double x, double y, double w, double h) from,
        (double x, double y, double w, double h) to)
    {
        var dx = to.x - from.x;
        var dy = to.y - from.y;
        var angle = Math.Atan2(dy, dx);

        var fromX = from.x + Math.Cos(angle) * from.w / 2;
        var fromY = from.y + Math.Sin(angle) * from.h / 2;
        var toX = to.x - Math.Cos(angle) * to.w / 2;
        var toY = to.y - Math.Sin(angle) * to.h / 2;

        builder.AddLine(
            fromX,
            fromY,
            toX,
            toY,
            stroke: "#666",
            strokeWidth: 1.5,
            strokeDasharray: "5,5");

        const int arrowSize = 8;
        const double arrowAngle = Math.PI / 6;
        var ax1 = toX - arrowSize * Math.Cos(angle - arrowAngle);
        var ay1 = toY - arrowSize * Math.Sin(angle - arrowAngle);
        var ax2 = toX - arrowSize * Math.Cos(angle + arrowAngle);
        var ay2 = toY - arrowSize * Math.Sin(angle + arrowAngle);

        builder.AddPath(
            string.Create(CultureInfo.InvariantCulture, $"M {toX:0.##} {toY:0.##} L {ax1:0.##} {ay1:0.##} L {ax2:0.##} {ay2:0.##} Z"),
            fill: "#666",
            stroke: "none");

        return ((fromX + toX) / 2, (fromY + toY) / 2 - 8);
    }

    /// <summary>
    /// Draws a relationship label centered at the given point on a white chip so
    /// it stays legible where it crosses lines or boxes.
    /// </summary>
    static void DrawLabelChip(
        SvgBuilder builder,
        double x,
        double y,
        string label,
        string? technology,
        RenderOptions options)
    {
        var fontSize = options.FontSize - 3;
        var techFontSize = options.FontSize - 4;
        var hasTech = !string.IsNullOrEmpty(technology);
        var techText = hasTech ? $"[{technology}]" : null;

        var width = Math.Max(
            label.Length * (fontSize * 0.6),
            hasTech ? techText!.Length * (techFontSize * 0.6) : 0) + 8;
        var lineHeight = fontSize + 4;
        var height = hasTech ? lineHeight * 2 + 2 : fontSize + 6;

        builder.AddRect(
            x - width / 2,
            y - height / 2.0,
            width,
            height,
            rx: 3,
            fill: "#FFFFFF",
            stroke: "none");

        builder.AddText(
            x,
            hasTech ? y - lineHeight / 2.0 + 1 : y,
            label,
            anchor: "middle",
            baseline: "middle",
            fontSize: fontSize,
            fontFamily: options.FontFamily,
            fill: "#666");

        if (hasTech)
        {
            builder.AddText(
                x,
                y + lineHeight / 2.0,
                techText!,
                anchor: "middle",
                baseline: "middle",
                fontSize: techFontSize,
                fontFamily: options.FontFamily,
                fill: "#888");
        }
    }

    static void DrawBoundaryBox(
        SvgBuilder builder,
        C4Boundary boundary,
        double x,
        double y,
        double width,
        double height,
        RenderOptions options)
    {
        builder.AddRect(
            x,
            y,
            width,
            height,
            rx: 5,
            fill: BoundaryFill,
            stroke: BoundaryStroke,
            strokeWidth: 2,
            style: "stroke-dasharray: 8 4");

        builder.AddText(
            x + width / 2,
            y + BoundaryTitleHeight / 2 - 5,
            boundary.Label,
            anchor: "middle",
            baseline: "middle",
            fontSize: options.FontSize,
            fontFamily: options.FontFamily,
            fontWeight: "bold",
            fill: "#333333");

        var typeLabel = boundary.Type switch
        {
            C4BoundaryType.Container => "[Container]",
            C4BoundaryType.System => "[System]",
            C4BoundaryType.Enterprise => "[Enterprise]",
            C4BoundaryType.Deployment => "[Deployment]",
            C4BoundaryType.Node => "[Node]",
            _ => ""
        };
        if (!string.IsNullOrEmpty(typeLabel))
        {
            builder.AddText(
                x + width / 2,
                y + BoundaryTitleHeight / 2 + 10,
                typeLabel,
                anchor: "middle",
                baseline: "middle",
                fontSize: options.FontSize - 3,
                fontFamily: options.FontFamily,
                fill: "#666666");
        }
    }

    static void DrawElement(SvgBuilder builder, C4Element element, double x, double y, RenderOptions options)
    {
        var color = GetElementColor(element);
        const string textColor = "#FFFFFF";

        if (element.Type == C4ElementType.Person)
        {
            // Draw person shape: a circular head sitting on top of a full-width
            // rounded body. The body spans the full element width so labels and
            // descriptions stay inside the shape.
            const int headRadius = 20;
            var centerX = x + ElementWidth / 2;
            var bodyTop = y + headRadius + 8;
            var bodyHeight = PersonHeight - (headRadius + 8);

            // Body first so the head circle overlaps its top edge (shoulders).
            builder.AddRect(
                x,
                bodyTop,
                ElementWidth,
                bodyHeight,
                rx: 8,
                fill: color,
                stroke: "none");

            // Head
            builder.AddCircle(
                centerX,
                y + headRadius,
                headRadius,
                fill: color,
                stroke: "none");

            // Center the text in the body region below the head.
            var textCenterY = (y + headRadius * 2 + (y + PersonHeight)) / 2;
            var hasDescription = !string.IsNullOrEmpty(element.Description);

            // Label
            builder.AddText(
                centerX,
                hasDescription ? textCenterY - 9 : textCenterY,
                element.Label,
                anchor: "middle",
                baseline: "middle",
                fontSize: options.FontSize - 1,
                fontFamily: options.FontFamily,
                fill: textColor,
                fontWeight: "bold");

            // Description
            if (hasDescription)
            {
                builder.AddText(
                    centerX,
                    textCenterY + 9,
                    TruncateText(element.Description!, 22),
                    anchor: "middle",
                    baseline: "middle",
                    fontSize: options.FontSize - 3,
                    fontFamily: options.FontFamily,
                    fill: textColor);
            }
        }
        else if (element.Type is
                 C4ElementType.ContainerDb or
                 C4ElementType.SystemDb)
        {
            // Draw database shape (cylinder)
            const int ellipseHeight = 15;

            // Top ellipse
            builder.AddEllipse(
                x + ElementWidth / 2,
                y + ellipseHeight,
                ElementWidth / 2 - 5,
                ellipseHeight,
                fill: color, stroke: "none");

            // Body
            builder.AddRect(
                x + 5,
                y + ellipseHeight,
                ElementWidth - 10,
                ElementHeight - ellipseHeight * 2,
                fill: color,
                stroke: "none");

            // Bottom ellipse
            builder.AddEllipse(
                x + ElementWidth / 2,
                y + ElementHeight - ellipseHeight,
                ElementWidth / 2 - 5,
                ellipseHeight,
                fill: color,
                stroke: "none");

            DrawElementText(builder, element, x, y, options, textColor);
        }
        else
        {
            // Standard box
            builder.AddRect(
                x,
                y,
                ElementWidth,
                ElementHeight,
                rx: 5,
                fill: color,
                stroke: "none");

            DrawElementText(builder, element, x, y, options, textColor);
        }
    }

    static void DrawElementText(
        SvgBuilder builder,
        C4Element element,
        double x,
        double y,
        RenderOptions options,
        string textColor)
    {
        var centerX = x + ElementWidth / 2;
        var textY = y + 25;

        // Label
        builder.AddText(
            centerX,
            textY,
            element.Label,
            anchor: "middle",
            baseline: "middle",
            fontSize: options.FontSize - 1,
            fontFamily: options.FontFamily,
            fill: textColor,
            fontWeight: "bold");

        // Technology
        if (!string.IsNullOrEmpty(element.Technology))
        {
            textY += 18;
            builder.AddText(
                centerX,
                textY,
                $"[{element.Technology}]",
                anchor: "middle",
                baseline: "middle",
                fontSize: options.FontSize - 3,
                fontFamily: options.FontFamily,
                fill: textColor);
        }

        // Description
        if (!string.IsNullOrEmpty(element.Description))
        {
            textY += 18;
            builder.AddText(
                centerX,
                textY,
                TruncateText(element.Description, 22),
                anchor: "middle",
                baseline: "middle",
                fontSize: options.FontSize - 3,
                fontFamily: options.FontFamily,
                fill: textColor);
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
            C4ElementType.SystemDb => SystemDbColor,
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

    /// <summary>
    /// Concrete graph model used to feed C4 elements and relationships to the
    /// shared layout engine.
    /// </summary>
    sealed class C4LayoutGraph : GraphDiagramBase
    {
    }

    /// <summary>
    /// Result of laying out one container: its content size and each direct
    /// member's center relative to the content's top-left corner.
    /// </summary>
    sealed class ContainerLayout
    {
        public double ContentWidth { get; set; }
        public double ContentHeight { get; set; }
        public Dictionary<string, (double X, double Y)> MemberCenters { get; } = new();

        // Engine-routed polylines for edges laid out within this container,
        // keyed by (source, target), in content-relative coordinates.
        public Dictionary<(string From, string To), List<Position>> EdgeRoutes { get; } = new();
    }
}
