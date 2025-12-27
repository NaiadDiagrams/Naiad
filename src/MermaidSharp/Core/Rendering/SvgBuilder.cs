using MermaidSharp.Models;

namespace MermaidSharp.Rendering;

public class SvgBuilder
{
    readonly SvgDocument _document = new();
    SvgGroup? _currentGroup;

    public SvgBuilder Size(double width, double height)
    {
        _document.Width = width;
        _document.Height = height;
        return this;
    }

    public SvgBuilder AddStyles(string css)
    {
        _document.CssStyles = css;
        return this;
    }

    public SvgBuilder AddMarker(string id, string path, double width, double height,
        double refX, double refY, string? fill = null)
    {
        _document.Defs.Markers.Add(new SvgMarker
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

    public SvgBuilder AddArrowMarker(string id = "arrowhead", string fill = "#333")
    {
        return AddMarker(id, "M0,0 L10,3.5 L0,7 Z", 10, 7, 9, 3.5, fill);
    }

    public SvgBuilder AddCircleMarker(string id = "circle", string fill = "#333")
    {
        _document.Defs.Markers.Add(new SvgMarker
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
        _document.Defs.Markers.Add(new SvgMarker
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

    public SvgBuilder BeginGroup(string? id = null, string? cssClass = null, string? transform = null)
    {
        var group = new SvgGroup
        {
            Id = id,
            Class = cssClass,
            Transform = transform
        };

        if (_currentGroup is not null)
        {
            _currentGroup.Children.Add(group);
        }
        else
        {
            _document.Elements.Add(group);
        }

        _currentGroup = group;
        return this;
    }

    public SvgBuilder EndGroup()
    {
        _currentGroup = null;
        return this;
    }

    public SvgBuilder AddRect(double x, double y, double width, double height,
        double rx = 0, string? fill = null, string? stroke = null, double? strokeWidth = null,
        string? id = null, string? cssClass = null)
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
            Class = cssClass
        };
        AddElement(rect);
        return this;
    }

    public SvgBuilder AddCircle(double cx, double cy, double r,
        string? fill = null, string? stroke = null, double? strokeWidth = null)
    {
        var circle = new SvgCircle
        {
            Cx = cx,
            Cy = cy,
            R = r,
            Fill = fill,
            Stroke = stroke,
            StrokeWidth = strokeWidth
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
        string? markerStart = null, string? markerEnd = null)
    {
        var path = new SvgPath
        {
            D = d,
            Fill = fill,
            Stroke = stroke,
            StrokeWidth = strokeWidth,
            StrokeDasharray = strokeDasharray,
            MarkerStart = markerStart,
            MarkerEnd = markerEnd
        };
        AddElement(path);
        return this;
    }

    public SvgBuilder AddPolygon(IEnumerable<Position> points,
        string? fill = null, string? stroke = null)
    {
        var polygon = new SvgPolygon { Fill = fill, Stroke = stroke };
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
        string? fill = null, string? id = null, string? cssClass = null)
    {
        var text = new SvgText
        {
            X = x,
            Y = y,
            Content = content,
            TextAnchor = anchor,
            DominantBaseline = baseline,
            FontSize = fontSize,
            FontFamily = fontFamily,
            FontWeight = fontWeight,
            Fill = fill,
            Id = id,
            Class = cssClass
        };
        AddElement(text);
        return this;
    }

    void AddElement(SvgElement element)
    {
        if (_currentGroup is not null)
        {
            _currentGroup.Children.Add(element);
        }
        else
        {
            _document.Elements.Add(element);
        }
    }

    public SvgDocument Build() => _document;
}
