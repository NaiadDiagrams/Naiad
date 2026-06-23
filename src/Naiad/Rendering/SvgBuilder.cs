namespace Naiad;

public class SvgBuilder
{
    SvgDocument document = new();
    Stack<SvgGroup> groupStack = new();
    double padding;
    double contentWidth;
    double contentHeight;
    bool allowHtmlElements = true;
    double labelFontSize = 14;
    string labelFontFamily = "Arial, sans-serif";

    // Threads render options that affect element emission (currently the HTML/no-HTML
    // label seam and the fonts used for the native-text fallback).
    public SvgBuilder Options(RenderOptions options)
    {
        allowHtmlElements = options.AllowHtmlElements;
        labelFontSize = options.FontSize;
        labelFontFamily = options.FontFamily;
        return this;
    }

    public SvgBuilder Size(double width, double height)
    {
        contentWidth = width;
        contentHeight = height;
        document.Width = width;
        document.Height = height;
        return this;
    }

    public SvgBuilder Padding(double padding)
    {
        this.padding = padding;
        // Adjust document size to include padding on all sides
        document.Width = contentWidth + padding * 2;
        document.Height = contentHeight + padding * 2;
        return this;
    }

    public SvgBuilder DiagramType(string diagramClass, string ariaRoledescription)
    {
        document.DiagramClass = diagramClass;
        document.AriaRoledescription = ariaRoledescription;
        return this;
    }

    public SvgBuilder AddStyles(string css)
    {
        document.CssStyles = css;
        return this;
    }

    public SvgBuilder AddMarker(
        string id,
        string path,
        double width,
        double height,
        double refX,
        double refY,
        string? fill = null)
    {
        document.Defs.Markers.Add(
            new()
            {
                Id = id,
                Path = path,
                MarkerWidth = width,
                MarkerHeight = height,
                RefX = refX,
                RefY = refY,
                Fill = fill
            });
        return this;
    }

    public SvgBuilder AddArrowMarker(
        string id = "arrowhead",
        string fill = "#333") =>
        AddMarker(id, "M0,0 L10,3.5 L0,7 Z", 10, 7, 9, 3.5, fill);

    public SvgBuilder AddCrossMarker(string id = "cross")
    {
        document.Defs.Markers.Add(
            new()
            {
                Id = id,
                Path = "M1,1 L7,7 M7,1 L1,7",
                MarkerWidth = 8,
                MarkerHeight = 8,
                RefX = 4,
                RefY = 4,
                Fill = "none"
            });
        return this;
    }

    public void AddMermaidArrowMarker()
    {
        document.Defs.Markers.Add(
            new()
            {
                Id = "naiad_flowchart-pointEnd",
                Path = "M 0 0 L 10 5 L 0 10 z",
                MarkerWidth = 8,
                MarkerHeight = 8,
                RefX = 5,
                RefY = 5,
                ViewBox = "0 0 10 10",
                MarkerUnits = "userSpaceOnUse",
                ClassName = "marker"
            });
        document.Defs.Markers.Add(
            new()
            {
                Id = "naiad_flowchart-pointStart",
                Path = "M 0 5 L 10 10 L 10 0 z",
                MarkerWidth = 8,
                MarkerHeight = 8,
                RefX = 4.5,
                RefY = 5,
                ViewBox = "0 0 10 10",
                MarkerUnits = "userSpaceOnUse",
                ClassName = "marker"
            });
    }

    public SvgBuilder AddMermaidCircleMarker()
    {
        document.Defs.Markers.Add(
            new()
            {
                Id = "naiad_flowchart-circleEnd",
                Path = "",
                UseCircle = true,
                CircleCx = 5,
                CircleCy = 5,
                CircleR = 5,
                MarkerWidth = 11,
                MarkerHeight = 11,
                RefX = 11,
                RefY = 5,
                ViewBox = "0 0 10 10",
                MarkerUnits = "userSpaceOnUse",
                ClassName = "marker"
            });
        document.Defs.Markers.Add(
            new()
            {
                Id = "naiad_flowchart-circleStart",
                Path = "",
                UseCircle = true,
                CircleCx = 5,
                CircleCy = 5,
                CircleR = 5,
                MarkerWidth = 11,
                MarkerHeight = 11,
                RefX = -1,
                RefY = 5,
                ViewBox = "0 0 10 10",
                MarkerUnits = "userSpaceOnUse",
                ClassName = "marker"
            });
        return this;
    }

    public SvgBuilder AddMermaidCrossMarker()
    {
        document.Defs.Markers.Add(
            new()
            {
                Id = "naiad_flowchart-crossEnd",
                Path = "M 1,1 l 9,9 M 10,1 l -9,9",
                MarkerWidth = 11,
                MarkerHeight = 11,
                RefX = 12,
                RefY = 5.2,
                ViewBox = "0 0 11 11",
                MarkerUnits = "userSpaceOnUse",
                ClassName = "marker cross",
                StrokeWidth = 2
            });
        document.Defs.Markers.Add(
            new()
            {
                Id = "naiad_flowchart-crossStart",
                Path = "M 1,1 l 9,9 M 10,1 l -9,9",
                MarkerWidth = 11,
                MarkerHeight = 11,
                RefX = -1,
                RefY = 5.2,
                ViewBox = "0 0 11 11",
                MarkerUnits = "userSpaceOnUse",
                ClassName = "marker cross",
                StrokeWidth = 2
            });
        return this;
    }

    /// <summary>Adds a <c>&lt;foreignObject&gt;</c> carrying raw label markup.</summary>
    /// <remarks>
    /// <paramref name="htmlContent"/> is pre-built XHTML, emitted verbatim (see
    /// <see cref="SvgForeignObject.HtmlContent"/>). Any user-supplied text within it must already be
    /// HTML-encoded by the caller.
    /// </remarks>
    public SvgBuilder AddForeignObject(
        double x,
        double y,
        double width,
        double height,
        string htmlContent,
        string? className = null)
    {
        var foreignObject = new SvgForeignObject
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            HtmlContent = htmlContent,
            Class = className
        };
        AddElement(foreignObject);
        return this;
    }

    /// <summary>
    /// Adds a box-centered label. With HTML allowed this is a <c>&lt;foreignObject&gt;</c> carrying the
    /// rich html; otherwise it is a native <c>&lt;text&gt;</c> built from the already-structured plainText
    /// (icons dropped, since they need a web font we do not embed). Empty labels emit nothing.
    /// </summary>
    /// <remarks>
    /// <paramref name="html"/> is pre-built XHTML used on the foreignObject path, emitted verbatim (see
    /// <see cref="SvgForeignObject.HtmlContent"/>). Any user-supplied text within it must already be
    /// HTML-encoded by the caller; the native-text fallback escapes <paramref name="plainText"/> itself.
    /// </remarks>
    public SvgBuilder AddLabel(
        double x,
        double y,
        double width,
        double height,
        string html,
        string plainText,
        string? className = null)
    {
        if (allowHtmlElements)
        {
            return AddForeignObject(x, y, width, height, html, className);
        }

        var text = plainText.Trim();
        if (text.Length == 0)
        {
            return this;
        }

        return AddText(
            x + width / 2,
            y + height / 2,
            text,
            anchor: "middle",
            baseline: "middle",
            fontSize: labelFontSize,
            fontFamily: labelFontFamily,
            cssClass: className);
    }

    public SvgBuilder BeginGroup(string? id = null, string? cssClass = null, string? transform = null)
    {
        var group = new SvgGroup
        {
            Id = id,
            Class = cssClass,
            Transform = transform
        };

        if (groupStack.TryPeek(out var parent))
        {
            parent.Children.Add(group);
        }
        else
        {
            document.Elements.Add(group);
        }

        groupStack.Push(group);
        return this;
    }

    public SvgBuilder EndGroup()
    {
        if (groupStack.Count > 0)
        {
            groupStack.Pop();
        }

        return this;
    }

    public SvgBuilder AddRect(
        double x,
        double y,
        double width,
        double height,
        double rx = 0,
        string? fill = null,
        string? stroke = null,
        double? strokeWidth = null,
        string? id = null,
        string? cssClass = null,
        string? style = null)
    {
        var rect = new SvgRect
        {
            X = x,
            Y = y,
            Width = width,
            Height = height,
            Rx = rx,
            Ry = rx,
            Fill = fill,
            Stroke = stroke,
            StrokeWidth = strokeWidth,
            Id = id,
            Class = cssClass,
            Style = style
        };
        AddElement(rect);
        return this;
    }

    public SvgBuilder AddRectNoXY(double width, double height, string? style = null)
    {
        var rect = new SvgRectNoXY
        {
            Width = width,
            Height = height,
            Style = style
        };
        AddElement(rect);
        return this;
    }

    public SvgBuilder AddCircle(
        double cx,
        double cy,
        double r,
        string? fill = null,
        string? stroke = null,
        double? strokeWidth = null,
        string? cssClass = null)
    {
        var circle = new SvgCircle
        {
            Cx = cx,
            Cy = cy,
            R = r,
            Fill = fill,
            Stroke = stroke,
            StrokeWidth = strokeWidth,
            Class = cssClass
        };
        AddElement(circle);
        return this;
    }

    public SvgBuilder AddEllipse(
        double cx,
        double cy,
        double rx,
        double ry,
        string? fill = null,
        string? stroke = null)
    {
        var ellipse = new SvgEllipse
        {
            Cx = cx,
            Cy = cy,
            Rx = rx,
            Ry = ry,
            Fill = fill,
            Stroke = stroke
        };
        AddElement(ellipse);
        return this;
    }

    public SvgBuilder AddLine(
        double x1,
        double y1,
        double x2,
        double y2,
        string? stroke = null,
        double? strokeWidth = null,
        string? strokeDasharray = null)
    {
        var line = new SvgLine
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = stroke,
            StrokeWidth = strokeWidth,
            StrokeDasharray = strokeDasharray
        };
        AddElement(line);
        return this;
    }

    public SvgBuilder AddPath(
        string d,
        string? fill = null,
        string? stroke = null,
        double? strokeWidth = null,
        string? strokeDasharray = null,
        string? markerStart = null,
        string? markerEnd = null,
        double? opacity = null,
        string? cssClass = null)
    {
        var path = new SvgPath
        {
            D = d,
            Fill = fill,
            Stroke = stroke,
            StrokeWidth = strokeWidth,
            StrokeDasharray = strokeDasharray,
            MarkerStart = markerStart,
            MarkerEnd = markerEnd,
            Opacity = opacity,
            Class = cssClass
        };
        AddElement(path);
        return this;
    }

    public SvgBuilder AddRawSvg(string markup)
    {
        AddElement(new SvgRaw {Markup = markup});
        return this;
    }

    public SvgBuilder AddPolygon(
        IEnumerable<Position> points,
        string? fill = null,
        string? stroke = null)
    {
        var polygon = new SvgPolygon {Fill = fill, Stroke = stroke};
        polygon.Points.AddRange(points);
        AddElement(polygon);
        return this;
    }

    public SvgBuilder AddText(
        double x,
        double y,
        string content,
        string? anchor = null,
        string? baseline = null,
        double? fontSize = null,
        string? fontFamily = null,
        string? fontWeight = null,
        string? fill = null,
        string? id = null,
        string? cssClass = null,
        string? transform = null,
        string? style = null,
        bool omitXY = false)
    {
        var text = new SvgText
        {
            X = x,
            Y = y,
            OmitXY = omitXY,
            Content = content,
            TextAnchor = anchor,
            DominantBaseline = baseline,
            FontSize = fontSize,
            FontFamily = fontFamily,
            FontWeight = fontWeight,
            Fill = fill,
            Id = id,
            Class = cssClass,
            Transform = transform,
            Style = style
        };
        AddElement(text);
        return this;
    }

    void AddElement(SvgElement element)
    {
        if (groupStack.TryPeek(out var parent))
        {
            parent.Children.Add(element);
        }
        else
        {
            document.Elements.Add(element);
        }
    }

    public SvgDocument Build()
    {
        // If padding is set, wrap all elements in a transform group
        if (padding > 0 &&
            document.Elements.Count > 0)
        {
            var paddingGroup = new SvgGroup
            {
                Transform = string.Create(CultureInfo.InvariantCulture, $"translate({padding:0.##},{padding:0.##})")
            };
            paddingGroup.Children.AddRange(document.Elements);
            document.Elements.Clear();
            document.Elements.Add(paddingGroup);
        }

        return document;
    }
}
