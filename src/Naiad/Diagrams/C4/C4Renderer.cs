namespace MermaidSharp.Diagrams.C4;

public class C4Renderer : IDiagramRenderer<C4Model>
{
    const double ElementWidth = 160;
    const double ElementHeight = 100;
    const double PersonHeight = 120;
    const double ElementSpacing = 30;
    const double TitleHeight = 50;
    const double RowSpacing = 40;
    const double BoundaryPadding = 15;
    const double BoundaryTitleHeight = 40;
    const double BoundarySpacing = 20;

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

    // Cached dimensions during rendering
    readonly Dictionary<string, (double w, double h)> _boundaryDimensions = new();
    readonly Dictionary<string, (double x, double y, double w, double h)> _elementPositions = new();
    readonly Dictionary<string, (double x, double y, double w, double h)> _boundaryPositions = new();

    public SvgDocument Render(C4Model model, RenderOptions options)
    {
        _boundaryDimensions.Clear();
        _elementPositions.Clear();
        _boundaryPositions.Clear();

        if (model.Elements.Count == 0 && model.Boundaries.Count == 0)
        {
            var emptyBuilder = new SvgBuilder().Size(200, 100);
            emptyBuilder.AddText(100, 50, "Empty C4 diagram", anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily);
            return emptyBuilder.Build();
        }

        // Step 1: Calculate all boundary dimensions (bottom-up)
        var topLevelBoundaries = model.Boundaries.Where(b => b.ParentBoundaryId == null).ToList();
        foreach (var boundary in topLevelBoundaries)
        {
            CalculateBoundaryDimensions(model, boundary);
        }

        // Step 2: Get elements outside any boundary
        var outsideElements = model.Elements.Where(e => e.BoundaryId == null).ToList();
        var outsidePersons = outsideElements.Where(e => e.Type == C4ElementType.Person).ToList();
        var outsideSystems = outsideElements.Where(e => e.Type is C4ElementType.System or C4ElementType.SystemDb).ToList();
        var outsideContainers = outsideElements.Where(e =>
            e.Type is C4ElementType.Container or C4ElementType.ContainerDb or C4ElementType.ContainerQueue).ToList();
        var outsideComponents = outsideElements.Where(e => e.Type == C4ElementType.Component).ToList();

        // Step 3: Calculate total diagram dimensions
        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : TitleHeight;

        // Calculate outside element rows
        var outsidePersonsHeight = outsidePersons.Count > 0 ? PersonHeight + RowSpacing : 0;
        var outsideSystemsHeight = outsideSystems.Count > 0 ? ElementHeight + RowSpacing : 0;
        var outsideContainersHeight = outsideContainers.Count > 0 ? ElementHeight + RowSpacing : 0;
        var outsideComponentsHeight = outsideComponents.Count > 0 ? ElementHeight + RowSpacing : 0;

        // Calculate top-level boundary row dimensions
        var boundaryRowWidth = topLevelBoundaries.Sum(b => _boundaryDimensions[b.Id].w + BoundarySpacing) - BoundarySpacing;
        var boundaryRowHeight = topLevelBoundaries.Count > 0
            ? topLevelBoundaries.Max(b => _boundaryDimensions[b.Id].h) + RowSpacing
            : 0;

        // Calculate width based on elements and boundaries
        var maxElementsPerRow = 4;
        var outsideElementsWidth = Math.Max(
            Math.Max(outsidePersons.Count, outsideSystems.Count),
            Math.Max(outsideContainers.Count, outsideComponents.Count)
        );
        outsideElementsWidth = Math.Min(outsideElementsWidth, maxElementsPerRow);
        var outsideWidth = outsideElementsWidth * (ElementWidth + ElementSpacing) - ElementSpacing;

        var width = Math.Max(Math.Max(outsideWidth, boundaryRowWidth), 400) + options.Padding * 2;
        var height = titleOffset + outsidePersonsHeight + outsideSystemsHeight +
                    boundaryRowHeight + outsideContainersHeight + outsideComponentsHeight +
                    options.Padding * 2 + 50;

        var builder = new SvgBuilder().Size(width, height);

        // Add arrow marker
        builder.AddArrowMarker("c4arrow", "#666");

        // Draw title
        if (!string.IsNullOrEmpty(model.Title))
        {
            builder.AddText(width / 2, options.Padding + TitleHeight / 2, model.Title,
                anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize + 6}px", fontFamily: options.FontFamily,
                fontWeight: "bold");
        }

        var currentY = options.Padding + titleOffset;

        // Draw outside persons
        currentY = DrawElementRow(builder, outsidePersons, currentY, width, options);

        // Draw outside systems
        currentY = DrawElementRow(builder, outsideSystems, currentY, width, options);

        // Draw top-level boundaries (recursively handles nested)
        if (topLevelBoundaries.Count > 0)
        {
            var boundaryStartX = (width - boundaryRowWidth) / 2;
            foreach (var boundary in topLevelBoundaries)
            {
                var (bw, bh) = _boundaryDimensions[boundary.Id];
                DrawBoundaryRecursive(builder, model, boundary, boundaryStartX, currentY, bw, bh, options);
                boundaryStartX += bw + BoundarySpacing;
            }
            currentY += topLevelBoundaries.Max(b => _boundaryDimensions[b.Id].h) + RowSpacing;
        }

        // Draw outside containers
        currentY = DrawElementRow(builder, outsideContainers, currentY, width, options);

        // Draw outside components
        DrawElementRow(builder, outsideComponents, currentY, width, options);

        // Draw relationships
        foreach (var rel in model.Relationships)
        {
            if (_elementPositions.TryGetValue(rel.From, out var fromPos) &&
                _elementPositions.TryGetValue(rel.To, out var toPos))
            {
                DrawRelationship(builder, fromPos, toPos, rel.Label, options);
            }
        }

        return builder.Build();
    }

    /// <summary>
    /// Recursively calculate boundary dimensions (bottom-up).
    /// </summary>
    (double w, double h) CalculateBoundaryDimensions(C4Model model, C4Boundary boundary)
    {
        // Get direct elements in this boundary
        var directElements = model.Elements.Where(e => e.BoundaryId == boundary.Id).ToList();

        // Get child boundaries
        var childBoundaries = model.Boundaries.Where(b => b.ParentBoundaryId == boundary.Id).ToList();

        // Recursively calculate child boundary dimensions first
        foreach (var child in childBoundaries)
        {
            CalculateBoundaryDimensions(model, child);
        }

        // Calculate content dimensions
        double contentWidth = 0;
        double contentHeight = 0;

        // Layout: child boundaries in a row, then direct elements below
        if (childBoundaries.Count > 0)
        {
            var childrenWidth = childBoundaries.Sum(b => _boundaryDimensions[b.Id].w + BoundarySpacing) - BoundarySpacing;
            var childrenHeight = childBoundaries.Max(b => _boundaryDimensions[b.Id].h);
            contentWidth = Math.Max(contentWidth, childrenWidth);
            contentHeight += childrenHeight + (directElements.Count > 0 ? RowSpacing : 0);
        }

        // Add direct elements (laid out in a row)
        if (directElements.Count > 0)
        {
            var elementsWidth = directElements.Count * (ElementWidth + ElementSpacing) - ElementSpacing;
            var elementsHeight = directElements.Max(e => e.Type == C4ElementType.Person ? PersonHeight : ElementHeight);
            contentWidth = Math.Max(contentWidth, elementsWidth);
            contentHeight += elementsHeight;
        }

        // Ensure minimum dimensions
        contentWidth = Math.Max(contentWidth, ElementWidth);
        contentHeight = Math.Max(contentHeight, ElementHeight);

        // Add boundary padding and title
        var totalWidth = contentWidth + BoundaryPadding * 2;
        var totalHeight = contentHeight + BoundaryPadding * 2 + BoundaryTitleHeight;

        _boundaryDimensions[boundary.Id] = (totalWidth, totalHeight);
        return (totalWidth, totalHeight);
    }

    /// <summary>
    /// Recursively draw a boundary and its contents.
    /// </summary>
    void DrawBoundaryRecursive(
        SvgBuilder builder,
        C4Model model,
        C4Boundary boundary,
        double x, double y,
        double width, double height,
        RenderOptions options)
    {
        // Draw boundary box
        builder.AddRect(x, y, width, height, rx: 5,
            fill: BoundaryFill, stroke: BoundaryStroke, strokeWidth: 2,
            style: "stroke-dasharray: 8 4");

        // Draw boundary label
        builder.AddText(x + width / 2, y + BoundaryTitleHeight / 2 - 5, boundary.Label,
            anchor: "middle", baseline: "middle",
            fontSize: $"{options.FontSize}px", fontFamily: options.FontFamily,
            fontWeight: "bold", fill: "#333333");

        // Draw boundary type indicator
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
            builder.AddText(x + width / 2, y + BoundaryTitleHeight / 2 + 10, typeLabel,
                anchor: "middle", baseline: "middle",
                fontSize: $"{options.FontSize - 3}px", fontFamily: options.FontFamily,
                fill: "#666666");
        }

        _boundaryPositions[boundary.Id] = (x + width / 2, y + height / 2, width, height);

        // Content area starts after title
        var contentY = y + BoundaryTitleHeight + BoundaryPadding;

        // Get child boundaries and direct elements
        var childBoundaries = model.Boundaries.Where(b => b.ParentBoundaryId == boundary.Id).ToList();
        var directElements = model.Elements.Where(e => e.BoundaryId == boundary.Id).ToList();

        // Draw child boundaries first (in a row)
        if (childBoundaries.Count > 0)
        {
            var childrenTotalWidth = childBoundaries.Sum(b => _boundaryDimensions[b.Id].w + BoundarySpacing) - BoundarySpacing;
            var childStartX = x + (width - childrenTotalWidth) / 2;

            foreach (var child in childBoundaries)
            {
                var (cw, ch) = _boundaryDimensions[child.Id];
                DrawBoundaryRecursive(builder, model, child, childStartX, contentY, cw, ch, options);
                childStartX += cw + BoundarySpacing;
            }

            // Move content Y down past child boundaries
            contentY += childBoundaries.Max(b => _boundaryDimensions[b.Id].h) + RowSpacing;
        }

        // Draw direct elements in this boundary
        if (directElements.Count > 0)
        {
            var elementsWidth = directElements.Count * (ElementWidth + ElementSpacing) - ElementSpacing;
            var startX = x + (width - elementsWidth) / 2;

            foreach (var element in directElements)
            {
                var eh = element.Type == C4ElementType.Person ? PersonHeight : ElementHeight;
                _elementPositions[element.Id] = (startX + ElementWidth / 2, contentY + eh / 2, ElementWidth, eh);
                DrawElement(builder, element, startX, contentY, options);
                startX += ElementWidth + ElementSpacing;
            }
        }
    }

    double DrawElementRow(
        SvgBuilder builder,
        List<C4Element> elements,
        double startY,
        double totalWidth,
        RenderOptions options)
    {
        if (elements.Count == 0)
        {
            return startY;
        }

        var rowWidth = elements.Count * (ElementWidth + ElementSpacing) - ElementSpacing;
        var startX = (totalWidth - rowWidth) / 2;

        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            var x = startX + i * (ElementWidth + ElementSpacing);
            var h = element.Type == C4ElementType.Person ? PersonHeight : ElementHeight;

            _elementPositions[element.Id] = (x + ElementWidth / 2, startY + h / 2, ElementWidth, h);
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
        else if (element.Type is C4ElementType.ContainerDb or C4ElementType.SystemDb)
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

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
