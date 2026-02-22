namespace MermaidSharp.Rendering;

public class SvgBuilder
{
    SvgDocument document = new();
    Stack<SvgGroup> groupStack = new();
    double padding;
    double contentWidth;
    double contentHeight;

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

    public SvgBuilder ViewBox(string viewBox)
    {
        document.ViewBoxOverride = viewBox;
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

    public SvgBuilder AddMarker(string id, string path, double width, double height,
        double refX, double refY, string? fill = null)
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

    public SvgBuilder AddArrowMarker(string id = "arrowhead", string fill = "#333") =>
        AddMarker(id, "M0,0 L10,3.5 L0,7 Z", 10, 7, 9, 3.5, fill);

    public SvgBuilder AddCircleMarker(string id = "circle", string fill = "#333")
    {
        document.Defs.Markers.Add(new()
        {
            Id = id,
            Path = "M4,4 m-3,0 a3,3 0 1,0 6,0 a3,3 0 1,0 -6,0",
            MarkerWidth = 8,
            MarkerHeight = 8,
            RefX = 4,
            RefY = 4,
            Fill = fill
        });
        return this;
    }

    public SvgBuilder AddCrossMarker(string id = "cross", string stroke = "#333")
    {
        document.Defs.Markers.Add(new()
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

    public SvgBuilder AddMermaidArrowMarker()
    {
        document.Defs.Markers.Add(
            new()
            {
                Id = "mermaid-svg_flowchart-v2-pointEnd",
                Path = "M 0 0 L 10 5 L 0 10 z",
                MarkerWidth = 8,
                MarkerHeight = 8,
                RefX = 5,
                RefY = 5,
                ViewBox = "0 0 10 10",
                MarkerUnits = "userSpaceOnUse",
                ClassName = "marker flowchart-v2"
            });
        document.Defs.Markers.Add(new()
        {
            Id = "mermaid-svg_flowchart-v2-pointStart",
            Path = "M 0 5 L 10 10 L 10 0 z",
            MarkerWidth = 8,
            MarkerHeight = 8,
            RefX = 4.5,
            RefY = 5,
            ViewBox = "0 0 10 10",
            MarkerUnits = "userSpaceOnUse",
            ClassName = "marker flowchart-v2"
        });
        return this;
    }

    public SvgBuilder AddMermaidCircleMarker()
    {
        document.Defs.Markers.Add(new()
        {
            Id = "mermaid-svg_flowchart-v2-circleEnd",
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
            ClassName = "marker flowchart-v2"
        });
        document.Defs.Markers.Add(new()
        {
            Id = "mermaid-svg_flowchart-v2-circleStart",
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
            ClassName = "marker flowchart-v2"
        });
        return this;
    }

    public SvgBuilder AddMermaidCrossMarker()
    {
        document.Defs.Markers.Add(new()
        {
            Id = "mermaid-svg_flowchart-v2-crossEnd",
            Path = "M 1,1 l 9,9 M 10,1 l -9,9",
            MarkerWidth = 11,
            MarkerHeight = 11,
            RefX = 12,
            RefY = 5.2,
            ViewBox = "0 0 11 11",
            MarkerUnits = "userSpaceOnUse",
            ClassName = "marker cross flowchart-v2",
            StrokeWidth = 2
        });
        document.Defs.Markers.Add(new()
        {
            Id = "mermaid-svg_flowchart-v2-crossStart",
            Path = "M 1,1 l 9,9 M 10,1 l -9,9",
            MarkerWidth = 11,
            MarkerHeight = 11,
            RefX = -1,
            RefY = 5.2,
            ViewBox = "0 0 11 11",
            MarkerUnits = "userSpaceOnUse",
            ClassName = "marker cross flowchart-v2",
            StrokeWidth = 2
        });
        return this;
    }

    public SvgBuilder AddForeignObject(double x, double y, double width, double height,
        string htmlContent, string? className = null)
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

    public SvgBuilder BeginGroup(string? id = null, string? cssClass = null, string? transform = null)
    {
        var group = new SvgGroup
        {
            Id = id,
            Class = cssClass,
            Transform = transform
        };

        if (groupStack.Count > 0)
        {
            groupStack.Peek().Children.Add(group);
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

    public SvgBuilder AddRect(double x, double y, double width, double height,
        double rx = 0, string? fill = null, string? stroke = null, double? strokeWidth = null,
        string? id = null, string? cssClass = null, string? style = null)
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

    public SvgBuilder AddCircle(double cx, double cy, double r,
        string? fill = null, string? stroke = null, double? strokeWidth = null, string? cssClass = null)
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

    public SvgBuilder AddEllipse(double cx, double cy, double rx, double ry,
        string? fill = null, string? stroke = null)
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

    public SvgBuilder AddLine(double x1, double y1, double x2, double y2,
        string? stroke = null, double? strokeWidth = null, string? strokeDasharray = null)
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

    public SvgBuilder AddPath(string d, string? fill = null, string? stroke = null,
        double? strokeWidth = null, string? strokeDasharray = null,
        string? markerStart = null, string? markerEnd = null, double? opacity = null,
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

    public SvgBuilder AddPolygon(IEnumerable<Position> points,
        string? fill = null, string? stroke = null)
    {
        var polygon = new SvgPolygon {Fill = fill, Stroke = stroke};
        polygon.Points.AddRange(points);
        AddElement(polygon);
        return this;
    }

    public SvgBuilder AddPolyline(IEnumerable<Position> points,
        string? fill = null, string? stroke = null, double? strokeWidth = null,
        string? strokeDasharray = null, string? markerEnd = null)
    {
        var polyline = new SvgPolyline
        {
            Fill = fill,
            Stroke = stroke,
            StrokeWidth = strokeWidth,
            StrokeDasharray = strokeDasharray,
            MarkerEnd = markerEnd
        };
        polyline.Points.AddRange(points);
        AddElement(polyline);
        return this;
    }

    public SvgBuilder AddText(double x, double y, string content,
        string? anchor = null, string? baseline = null,
        string? fontSize = null, string? fontFamily = null, string? fontWeight = null,
        string? fill = null, string? id = null, string? cssClass = null,
        string? transform = null, string? style = null, bool omitXY = false)
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
        if (groupStack.Count > 0)
        {
            groupStack.Peek().Children.Add(element);
        }
        else
        {
            document.Elements.Add(element);
        }
    }

    public SvgDocument Build()
    {
        // If padding is set, wrap all elements in a transform group
        if (padding > 0 && document.Elements.Count > 0)
        {
            var paddingGroup = new SvgGroup
            {
                Transform = $"translate({Fmt(padding)},{Fmt(padding)})"
            };
            paddingGroup.Children.AddRange(document.Elements);
            document.Elements.Clear();
            document.Elements.Add(paddingGroup);
        }

        return document;
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
