namespace Naiad.Diagrams.C4;

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
    const int MaxElementsPerRow = 4;

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
    readonly Dictionary<string, (double w, double h)> boundaryDimensions = new();
    readonly Dictionary<string, (double x, double y, double w, double h)> elementPositions = new();
    readonly Dictionary<string, (double x, double y, double w, double h)> boundaryPositions = new();

    public SvgDocument Render(C4Model model, RenderOptions options)
    {
        boundaryDimensions.Clear();
        elementPositions.Clear();
        boundaryPositions.Clear();

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

        // Step 1: Calculate all boundary dimensions (bottom-up)
        var topLevelBoundaries = model.Boundaries.Where(_ => _.ParentBoundaryId == null).ToList();
        foreach (var boundary in topLevelBoundaries)
        {
            CalculateBoundaryDimensions(model, boundary);
        }

        // Step 2: Get elements outside any boundary
        var outsideElements = model.Elements.Where(_ => _.BoundaryId == null).ToList();
        var outsidePersons = outsideElements.Where(_ => _.Type == C4ElementType.Person).ToList();
        var outsideSystems = outsideElements.Where(_ => _.Type is C4ElementType.System or C4ElementType.SystemDb).ToList();
        var outsideContainers = outsideElements.Where(_ =>
            _.Type is C4ElementType.Container or C4ElementType.ContainerDb or C4ElementType.ContainerQueue).ToList();
        var outsideComponents = outsideElements.Where(_ => _.Type == C4ElementType.Component).ToList();

        // Step 3: Calculate total diagram dimensions
        var titleOffset = string.IsNullOrEmpty(model.Title) ? 0 : TitleHeight;

        // Calculate outside element rows. Each kind wraps at MaxElementsPerRow,
        // so its block height grows with the number of wrapped rows.
        var outsidePersonsHeight = RowCount(outsidePersons.Count) * (PersonHeight + RowSpacing);
        var outsideSystemsHeight = RowCount(outsideSystems.Count) * (ElementHeight + RowSpacing);
        var outsideContainersHeight = RowCount(outsideContainers.Count) * (ElementHeight + RowSpacing);
        var outsideComponentsHeight = RowCount(outsideComponents.Count) * (ElementHeight + RowSpacing);

        // Calculate top-level boundary row dimensions
        var boundaryRowWidth = topLevelBoundaries.Sum(_ => boundaryDimensions[_.Id].w + BoundarySpacing) - BoundarySpacing;
        var boundaryRowHeight = topLevelBoundaries.Count > 0
            ? topLevelBoundaries.Max(_ => boundaryDimensions[_.Id].h) + RowSpacing
            : 0;

        // Calculate width based on elements and boundaries. Rows wrap at
        // MaxElementsPerRow, so the widest possible row caps at that many.
        var outsideElementsWidth = Math.Max(
            Math.Max(outsidePersons.Count, outsideSystems.Count),
            Math.Max(outsideContainers.Count, outsideComponents.Count)
        );
        outsideElementsWidth = Math.Min(outsideElementsWidth, MaxElementsPerRow);
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
            builder.AddText(
                width / 2,
                options.Padding + TitleHeight / 2,
                model.Title,
                anchor: "middle",
                baseline: "middle",
                fontSize: options.FontSize + 6,
                fontFamily: options.FontFamily,
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
                var (bw, bh) = boundaryDimensions[boundary.Id];
                DrawBoundaryRecursive(builder, model, boundary, boundaryStartX, currentY, bw, bh, options);
                boundaryStartX += bw + BoundarySpacing;
            }
            currentY += topLevelBoundaries.Max(_ => boundaryDimensions[_.Id].h) + RowSpacing;
        }

        // Draw outside containers
        currentY = DrawElementRow(builder, outsideContainers, currentY, width, options);

        // Draw outside components
        DrawElementRow(builder, outsideComponents, currentY, width, options);

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

    /// <summary>
    /// Recursively calculate boundary dimensions (bottom-up).
    /// </summary>
    (double w, double h) CalculateBoundaryDimensions(C4Model model, C4Boundary boundary)
    {
        // Get direct elements in this boundary
        var directElements = model.Elements.Where(_ => _.BoundaryId == boundary.Id).ToList();

        // Get child boundaries
        var childBoundaries = model.Boundaries.Where(_ => _.ParentBoundaryId == boundary.Id).ToList();

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
            var childrenWidth = childBoundaries.Sum(_ => boundaryDimensions[_.Id].w + BoundarySpacing) - BoundarySpacing;
            var childrenHeight = childBoundaries.Max(_ => boundaryDimensions[_.Id].h);
            contentWidth = Math.Max(contentWidth, childrenWidth);
            contentHeight += childrenHeight + (directElements.Count > 0 ? RowSpacing : 0);
        }

        // Add direct elements (laid out in a row)
        if (directElements.Count > 0)
        {
            var elementsWidth = directElements.Count * (ElementWidth + ElementSpacing) - ElementSpacing;
            var elementsHeight = directElements.Max(_ => _.Type == C4ElementType.Person ? PersonHeight : ElementHeight);
            contentWidth = Math.Max(contentWidth, elementsWidth);
            contentHeight += elementsHeight;
        }

        // Ensure minimum dimensions
        contentWidth = Math.Max(contentWidth, ElementWidth);
        contentHeight = Math.Max(contentHeight, ElementHeight);

        // Add boundary padding and title
        var totalWidth = contentWidth + BoundaryPadding * 2;
        var totalHeight = contentHeight + BoundaryPadding * 2 + BoundaryTitleHeight;

        boundaryDimensions[boundary.Id] = (totalWidth, totalHeight);
        return (totalWidth, totalHeight);
    }

    /// <summary>
    /// Recursively draw a boundary and its contents.
    /// </summary>
    void DrawBoundaryRecursive(
        SvgBuilder builder,
        C4Model model,
        C4Boundary boundary,
        double x,
        double y,
        double width,
        double height,
        RenderOptions options)
    {
        // Draw boundary box
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

        // Draw boundary label
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

        boundaryPositions[boundary.Id] = (x + width / 2, y + height / 2, width, height);

        // Content area starts after title
        var contentY = y + BoundaryTitleHeight + BoundaryPadding;

        // Get child boundaries and direct elements
        var childBoundaries = model.Boundaries.Where(_ => _.ParentBoundaryId == boundary.Id).ToList();
        var directElements = model.Elements.Where(_ => _.BoundaryId == boundary.Id).ToList();

        // Draw child boundaries first (in a row)
        if (childBoundaries.Count > 0)
        {
            var childrenTotalWidth = childBoundaries.Sum(_ => boundaryDimensions[_.Id].w + BoundarySpacing) - BoundarySpacing;
            var childStartX = x + (width - childrenTotalWidth) / 2;

            foreach (var child in childBoundaries)
            {
                var (cw, ch) = boundaryDimensions[child.Id];
                DrawBoundaryRecursive(builder, model, child, childStartX, contentY, cw, ch, options);
                childStartX += cw + BoundarySpacing;
            }

            // Move content Y down past child boundaries
            contentY += childBoundaries.Max(_ => boundaryDimensions[_.Id].h) + RowSpacing;
        }

        // Draw direct elements in this boundary
        if (directElements.Count > 0)
        {
            var elementsWidth = directElements.Count * (ElementWidth + ElementSpacing) - ElementSpacing;
            var startX = x + (width - elementsWidth) / 2;

            foreach (var element in directElements)
            {
                var eh = element.Type == C4ElementType.Person ? PersonHeight : ElementHeight;
                elementPositions[element.Id] = (startX + ElementWidth / 2, contentY + eh / 2, ElementWidth, eh);
                DrawElement(builder, element, startX, contentY, options);
                startX += ElementWidth + ElementSpacing;
            }
        }
    }

    /// <summary>
    /// Number of wrapped rows needed to lay out <paramref name="count"/> elements.
    /// </summary>
    static int RowCount(int count) => (count + MaxElementsPerRow - 1) / MaxElementsPerRow;

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

        var currentY = startY;

        // Wrap at MaxElementsPerRow so wide rows flow onto subsequent rows
        // instead of overflowing the canvas width.
        for (var rowStart = 0; rowStart < elements.Count; rowStart += MaxElementsPerRow)
        {
            var rowCount = Math.Min(MaxElementsPerRow, elements.Count - rowStart);
            var rowWidth = rowCount * (ElementWidth + ElementSpacing) - ElementSpacing;
            var startX = (totalWidth - rowWidth) / 2;

            var maxHeight = ElementHeight;
            for (var i = 0; i < rowCount; i++)
            {
                var element = elements[rowStart + i];
                var x = startX + i * (ElementWidth + ElementSpacing);
                var h = element.Type == C4ElementType.Person ? PersonHeight : ElementHeight;
                maxHeight = Math.Max(maxHeight, h);

                elementPositions[element.Id] = (x + ElementWidth / 2, currentY + h / 2, ElementWidth, h);
                DrawElement(builder, element, x, currentY, options);
            }

            currentY += maxHeight + RowSpacing;
        }

        return currentY;
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

    static void DrawRelationship(
        SvgBuilder builder,
        (double x, double y, double w, double h) from,
        (double x, double y, double w, double h) to,
        string? label,
        RenderOptions options)
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
        builder.AddLine(
            fromX,
            fromY,
            toX,
            toY,
            stroke: "#666",
            strokeWidth: 1.5,
            strokeDasharray: "5,5");

        // Draw arrowhead manually
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

        // Draw label
        if (!string.IsNullOrEmpty(label))
        {
            var midX = (fromX + toX) / 2;
            var midY = (fromY + toY) / 2 - 8;
            var fontSize = options.FontSize - 3;

            // White chip behind the label so it stays legible where a line
            // crosses an element box or another relationship.
            var labelWidth = label.Length * (fontSize * 0.6) + 8;
            var labelHeight = fontSize + 6;
            builder.AddRect(
                midX - labelWidth / 2,
                midY - labelHeight / 2.0,
                labelWidth,
                labelHeight,
                rx: 3,
                fill: "#FFFFFF",
                stroke: "none");

            builder.AddText(
                midX,
                midY,
                label,
                anchor: "middle",
                baseline: "middle",
                fontSize: fontSize,
                fontFamily: options.FontFamily,
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

}
