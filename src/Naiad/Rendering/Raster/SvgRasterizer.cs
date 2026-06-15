/// <summary>
/// The shared SVG → raster pipeline. Walks an <see cref="SvgDocument"/> once — resolving the CSS
/// cascade, composing transforms, flattening geometry, drawing markers and laying out label text — and
/// paints the result into a caller-supplied <see cref="IRenderSurface"/>. This is the single place all
/// the rendering intelligence lives; the Naiad.Skia and Naiad.ImageSharp packages add only a thin
/// surface that maps the primitives onto their rasterizer, so the two backends render identically bar
/// antialiasing and font shaping.
/// </summary>
static class SvgRasterizer
{
    /// <summary>
    /// Renders <paramref name="document"/> into a freshly created surface and returns it. The surface
    /// is sized from the document's viewBox scaled by <paramref name="scale"/>;
    /// <paramref name="createSurface"/> is handed that pixel size and must return a surface of exactly
    /// those dimensions (already cleared to the desired background).
    /// </summary>
    public static TSurface Paint<TSurface>(SvgDocument document, double scale, Func<int, int, TSurface> createSurface)
        where TSurface : IRenderSurface
    {
        var (minX, minY, viewWidth, viewHeight) = ParseViewBox(document);
        var width = Math.Max(1, (int)Math.Ceiling(viewWidth * scale));
        var height = Math.Max(1, (int)Math.Ceiling(viewHeight * scale));
        var surface = createSurface(width, height);

        // Map user space → device: shift the viewBox origin to (0,0) then scale up.
        var baseTransform = Matrix3x2.CreateTranslation(-(float)minX, -(float)minY) * Matrix3x2.CreateScale((float)scale);

        var context = new Context(surface, Stylesheet.Parse(document.CssStyles), document.Defs);
        var rootMatch = new ElementMatch("svg", document.Id, []);
        var rootStyle = context.ResolveRootStyle(rootMatch);
        var chain = new List<ElementMatch> {rootMatch};
        foreach (var element in document.Elements)
        {
            context.Walk(element, chain, rootStyle, baseTransform, 1f);
        }

        return surface;
    }

    static (double MinX, double MinY, double Width, double Height) ParseViewBox(SvgDocument document)
    {
        var parts = document.ViewBox.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 4 &&
            double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var minX) &&
            double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var minY) &&
            double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var width) &&
            double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var height) &&
            width > 0 && height > 0)
        {
            return (minX, minY, width, height);
        }

        return (0, 0, Math.Max(1, document.Width), Math.Max(1, document.Height));
    }

    sealed class Context(IRenderSurface surface, Stylesheet stylesheet, SvgDefs defs)
    {
        readonly Dictionary<string, SvgMarker> markers = BuildMarkerLookup(defs);

        public ComputedStyle ResolveRootStyle(ElementMatch rootMatch)
        {
            var style = new ComputedStyle();
            ApplyCascade(style, [rootMatch], presentation: null, inlineStyle: null);
            return style;
        }

        public void Walk(SvgElement element, List<ElementMatch> chain, ComputedStyle inherited, Matrix3x2 transform, float groupOpacity)
        {
            var ctm = SvgTransform.Parse(element.Transform) * transform;
            var match = MatchFor(element);
            chain.Add(match);
            try
            {
                var style = inherited.CloneForChild();
                ApplyCascade(style, chain, Presentation(element), element.Style);
                var opacity = groupOpacity * (float)style.Opacity;

                switch (element)
                {
                    case SvgGroup group:
                        foreach (var child in group.Children)
                        {
                            Walk(child, chain, style, ctm, opacity);
                        }

                        break;
                    case SvgRect rect:
                        DrawShape(BuildRect(rect.X, rect.Y, rect.Width, rect.Height, rect.Rx, rect.Ry), ctm, style, opacity);
                        break;
                    case SvgRectNoXY rect:
                        DrawShape(BuildRect(0, 0, rect.Width, rect.Height, 0, 0), ctm, style, opacity);
                        break;
                    case SvgCircle circle:
                        DrawShape(BuildEllipse(circle.Cx, circle.Cy, circle.R, circle.R), ctm, style, opacity);
                        break;
                    case SvgEllipse ellipse:
                        DrawShape(BuildEllipse(ellipse.Cx, ellipse.Cy, ellipse.Rx, ellipse.Ry), ctm, style, opacity);
                        break;
                    case SvgLine line:
                        DrawShape([new([new(ToF(line.X1), ToF(line.Y1)), new(ToF(line.X2), ToF(line.Y2))], false)], ctm, style, opacity);
                        break;
                    case SvgPolygon polygon:
                        DrawShape([new(polygon.Points.Select(_ => new Vector2(ToF(_.X), ToF(_.Y))).ToList(), true)], ctm, style, opacity);
                        break;
                    case SvgPath path:
                        var subpaths = PathFlattener.Flatten(path.D);
                        DrawShape(subpaths, ctm, style, opacity);
                        DrawMarkers(path, subpaths, ctm, chain);
                        break;
                    case SvgText text:
                        DrawText(text, ctm, style, opacity);
                        break;
                    case SvgForeignObject foreignObject:
                        DrawForeignObject(foreignObject, ctm, style, opacity);
                        break;
                    case SvgRaw raw:
                        DrawRaw(raw.Markup, style, ctm, opacity);
                        break;
                }
            }
            finally
            {
                chain.RemoveAt(chain.Count - 1);
            }
        }

        // Fills then strokes a shape's contours using the element's resolved style.
        public void DrawShape(IReadOnlyList<SubPath> subpaths, Matrix3x2 ctm, ComputedStyle style, float opacity)
        {
            if (subpaths.Count == 0)
            {
                return;
            }

            if (ResolveFill(style.Fill, style, subpaths) is { } fill)
            {
                surface.FillPath(subpaths, ctm, fill, FillRule.NonZero, opacity * (float)style.FillOpacity);
            }

            if (style.StrokeWidth > 0 && ResolveColor(style.Stroke, style) is { } stroke)
            {
                surface.StrokePath(subpaths, ctm, stroke, (float)style.StrokeWidth, ParseDash(style.StrokeDasharray), opacity * (float)style.StrokeOpacity);
            }
        }

        void DrawText(SvgText text, Matrix3x2 ctm, ComputedStyle style, float opacity)
        {
            if (string.IsNullOrEmpty(text.Content))
            {
                return;
            }

            var color = ResolveColor(style.Fill, style) ?? Rgba.Black;
            surface.DrawText(text.Content, text.OmitXY ? 0 : ToF(text.X), text.OmitXY ? 0 : ToF(text.Y), ctm, TextStyleFrom(style, color, opacity));
        }

        void DrawForeignObject(SvgForeignObject foreignObject, Matrix3x2 ctm, ComputedStyle style, float opacity)
        {
            var lines = HtmlText.ExtractLines(foreignObject.HtmlContent);
            if (lines.Count == 0)
            {
                return;
            }

            var color = ResolveColor(style.Fill, style) ?? new Rgba(0x33, 0x33, 0x33, 255);
            var textStyle = TextStyleFrom(style, color, opacity) with {Anchor = TextAnchorKind.Middle, Baseline = TextBaselineKind.Middle};

            var centerX = ToF(foreignObject.X + foreignObject.Width / 2);
            var centerY = ToF(foreignObject.Y + foreignObject.Height / 2);
            var lineHeight = textStyle.FontSize * 1.2f;
            var top = centerY - (lines.Count - 1) * lineHeight / 2;
            for (var i = 0; i < lines.Count; i++)
            {
                surface.DrawText(lines[i], centerX, top + i * lineHeight, ctm, textStyle);
            }
        }

        // Renders inline SVG markup (an iconify icon body wrapped in a styled <g>). There's no CSS to
        // cascade here, so styling comes from attributes, inline style and inheritance only. Unknown
        // elements are descended into rather than dropped, so wrapper/container tags are transparent.
        void DrawRaw(string markup, ComputedStyle inherited, Matrix3x2 ctm, float opacity)
        {
            XDocument document;
            try
            {
                document = XDocument.Parse($"<svg xmlns=\"http://www.w3.org/2000/svg\">{markup}</svg>");
            }
            catch (XmlException)
            {
                // Malformed icon body — skip rather than fail the whole render.
                return;
            }

            foreach (var child in document.Root!.Elements())
            {
                WalkXml(child, inherited, ctm, opacity);
            }
        }

        void WalkXml(XElement element, ComputedStyle inherited, Matrix3x2 transform, float opacity)
        {
            var ctm = SvgTransform.Parse((string?)element.Attribute("transform")) * transform;
            var style = inherited.CloneForChild();
            foreach (var attribute in element.Attributes())
            {
                style.Apply(attribute.Name.LocalName.ToLowerInvariant(), attribute.Value);
            }

            foreach (var (property, value, _) in InlineDeclarations((string?)element.Attribute("style")))
            {
                style.Apply(property, value);
            }

            var elementOpacity = opacity * (float)style.Opacity;
            switch (element.Name.LocalName)
            {
                case "path":
                    DrawShape(PathFlattener.Flatten((string?)element.Attribute("d")), ctm, style, elementOpacity);
                    break;
                case "rect":
                    DrawShape(BuildRect(XmlNum(element, "x"), XmlNum(element, "y"), XmlNum(element, "width"), XmlNum(element, "height"), XmlNum(element, "rx"), XmlNum(element, "ry")), ctm, style, elementOpacity);
                    break;
                case "circle":
                    DrawShape(BuildEllipse(XmlNum(element, "cx"), XmlNum(element, "cy"), XmlNum(element, "r"), XmlNum(element, "r")), ctm, style, elementOpacity);
                    break;
                case "ellipse":
                    DrawShape(BuildEllipse(XmlNum(element, "cx"), XmlNum(element, "cy"), XmlNum(element, "rx"), XmlNum(element, "ry")), ctm, style, elementOpacity);
                    break;
                case "line":
                    DrawShape([new([new(ToF(XmlNum(element, "x1")), ToF(XmlNum(element, "y1"))), new(ToF(XmlNum(element, "x2")), ToF(XmlNum(element, "y2")))], false)], ctm, style, elementOpacity);
                    break;
                case "polygon":
                    DrawShape([new(XmlPoints(element), true)], ctm, style, elementOpacity);
                    break;
                case "polyline":
                    DrawShape([new(XmlPoints(element), false)], ctm, style, elementOpacity);
                    break;
                default:
                    // svg/g/symbol and anything else: transparent container — descend.
                    foreach (var child in element.Elements())
                    {
                        WalkXml(child, style, ctm, elementOpacity);
                    }

                    break;
            }
        }

        static double XmlNum(XElement element, string name) =>
            double.TryParse((string?)element.Attribute(name), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0;

        static List<Vector2> XmlPoints(XElement element)
        {
            var points = new List<Vector2>();
            var raw = (string?)element.Attribute("points");
            if (raw == null)
            {
                return points;
            }

            var numbers = raw.Split([' ', ',', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i + 1 < numbers.Length; i += 2)
            {
                if (double.TryParse(numbers[i], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                    double.TryParse(numbers[i + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                {
                    points.Add(new((float)x, (float)y));
                }
            }

            return points;
        }

        void DrawMarkers(SvgPath path, IReadOnlyList<SubPath> subpaths, Matrix3x2 ctm, List<ElementMatch> chain)
        {
            if (path.MarkerStart is { } start && MarkerLookup(start) is { } startMarker &&
                EndpointDirection(subpaths, atStart: true) is var (startPoint, startAngle))
            {
                DrawMarker(startMarker, startPoint, startAngle, ctm, chain);
            }

            if (path.MarkerEnd is { } end && MarkerLookup(end) is { } endMarker &&
                EndpointDirection(subpaths, atStart: false) is var (endPoint, endAngle))
            {
                DrawMarker(endMarker, endPoint, endAngle, ctm, chain);
            }
        }

        void DrawMarker(SvgMarker marker, Vector2 vertex, float angle, Matrix3x2 pathCtm, List<ElementMatch> chain)
        {
            var (vbMinX, vbMinY, vbWidth, vbHeight) = ParseMarkerViewBox(marker);
            var scaleX = vbWidth > 0 ? (float)(marker.MarkerWidth / vbWidth) : 1;
            var scaleY = vbHeight > 0 ? (float)(marker.MarkerHeight / vbHeight) : 1;

            // marker content space → path-local space: shift refX/refY to the origin, scale into the
            // marker box, rotate to the path direction, then drop onto the path vertex.
            var markerLocal =
                Matrix3x2.CreateTranslation(-(float)(marker.RefX + vbMinX), -(float)(marker.RefY + vbMinY)) *
                Matrix3x2.CreateScale(scaleX, scaleY) *
                Matrix3x2.CreateRotation(angle) *
                Matrix3x2.CreateTranslation(vertex.X, vertex.Y);
            var ctm = markerLocal * pathCtm;

            var (fill, stroke) = MarkerColors(marker, chain);
            var strokeWidth = (float)marker.StrokeWidth;

            IReadOnlyList<SubPath> content = marker.UseCircle
                ? BuildEllipse(marker.CircleCx, marker.CircleCy, marker.CircleR, marker.CircleR)
                : PathFlattener.Flatten(marker.Path);
            if (content.Count == 0)
            {
                return;
            }

            if (fill is { } fillColor)
            {
                surface.FillPath(content, ctm, new SolidPaint(fillColor), FillRule.NonZero, 1f);
            }

            if (stroke is { } strokeColor && strokeWidth > 0)
            {
                surface.StrokePath(content, ctm, strokeColor, strokeWidth, dash: null, 1f);
            }
        }

        (Rgba? Fill, Rgba? Stroke) MarkerColors(SvgMarker marker, List<ElementMatch> chain)
        {
            // Resolve the marker's own fill/stroke as if it were an element under the root, so the
            // `.marker { fill:#333333 }` rule (inherited by the marker's content) is honoured.
            var markerMatch = new ElementMatch("marker", marker.Id, Split(marker.ClassName));
            var style = new ComputedStyle();
            ApplyCascade(style, [chain[0], markerMatch], presentation: null, inlineStyle: null);
            if (marker.Fill != null)
            {
                style.Fill = marker.Fill;
            }

            var fill = ResolveColor(style.Fill, style) ?? new Rgba(0x33, 0x33, 0x33, 255);
            var stroke = ResolveColor(style.Stroke, style) ?? fill;
            return (fill, stroke);
        }

        SvgMarker? MarkerLookup(string reference)
        {
            var id = ExtractUrlId(reference);
            return id != null && markers.TryGetValue(id, out var marker) ? marker : null;
        }

        // --- style resolution -------------------------------------------------------------------

        void ApplyCascade(ComputedStyle style, IReadOnlyList<ElementMatch> chain, IReadOnlyList<(string, string)>? presentation, string? inlineStyle)
        {
            // 1. presentation attributes (lowest author priority).
            if (presentation != null)
            {
                foreach (var (property, value) in presentation)
                {
                    style.Apply(property, value);
                }
            }

            var matched = stylesheet.Match(chain);
            var inline = InlineDeclarations(inlineStyle);

            // 2. normal stylesheet declarations, ascending specificity then source order.
            foreach (var declaration in Ordered(matched, important: false))
            {
                style.Apply(declaration.Property, declaration.Value);
            }

            // 3. normal inline declarations.
            foreach (var (property, value, important) in inline)
            {
                if (!important)
                {
                    style.Apply(property, value);
                }
            }

            // 4. important stylesheet declarations, then 5. important inline — these top the cascade.
            foreach (var declaration in Ordered(matched, important: true))
            {
                style.Apply(declaration.Property, declaration.Value);
            }

            foreach (var (property, value, important) in inline)
            {
                if (important)
                {
                    style.Apply(property, value);
                }
            }
        }

        static IEnumerable<MatchedDeclaration> Ordered(List<MatchedDeclaration> matched, bool important) =>
            matched
                .Where(_ => _.Important == important)
                .OrderBy(_ => _.Specificity)
                .ThenBy(_ => _.Order);

        Paint? ResolveFill(string? raw, ComputedStyle style, IReadOnlyList<SubPath> subpaths)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (raw.StartsWith("url(", StringComparison.OrdinalIgnoreCase))
            {
                return ResolveGradient(ExtractUrlId(raw), subpaths);
            }

            if (raw.Equals("currentColor", StringComparison.OrdinalIgnoreCase))
            {
                return CssColor.TryParse(style.Color, out var current) ? new SolidPaint(current) : null;
            }

            return CssColor.TryParse(raw, out var color) ? new SolidPaint(color) : null;
        }

        Paint? ResolveGradient(string? id, IReadOnlyList<SubPath> subpaths)
        {
            if (id == null)
            {
                return null;
            }

            var gradient = defs.Gradients.FirstOrDefault(_ => _.Id == id);
            if (gradient == null || gradient.Stops.Count == 0)
            {
                return null;
            }

            var stops = gradient.Stops
                .Select(_ => new GradientStop((float)(_.Offset / 100), CssColor.TryParse(_.Color, out var c) ? c : Rgba.Black))
                .ToList();

            var (min, max) = Bounds(subpaths);
            if (gradient.IsRadial)
            {
                var center = new Vector2((min.X + max.X) / 2, (min.Y + max.Y) / 2);
                return new RadialGradientPaint(center, Math.Max(max.X - min.X, max.Y - min.Y) / 2, stops);
            }

            var midY = (min.Y + max.Y) / 2;
            return new LinearGradientPaint(new(min.X, midY), new(max.X, midY), stops);
        }

        static Rgba? ResolveColor(string? raw, ComputedStyle style)
        {
            if (string.IsNullOrWhiteSpace(raw) || raw.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (raw.Equals("currentColor", StringComparison.OrdinalIgnoreCase))
            {
                return CssColor.TryParse(style.Color, out var current) ? current : null;
            }

            return CssColor.TryParse(raw, out var color) ? color : null;
        }

        static TextStyle TextStyleFrom(ComputedStyle style, Rgba color, float opacity) =>
            new()
            {
                FontFamilies = FontFamilies(style.FontFamily),
                FontSize = (float)style.FontSize,
                Bold = IsBold(style.FontWeight),
                Italic = string.Equals(style.FontStyle, "italic", StringComparison.OrdinalIgnoreCase),
                Color = color,
                Anchor = style.TextAnchor switch
                {
                    "middle" => TextAnchorKind.Middle,
                    "end" => TextAnchorKind.End,
                    _ => TextAnchorKind.Start,
                },
                Baseline = style.DominantBaseline switch
                {
                    "middle" or "central" => TextBaselineKind.Middle,
                    "hanging" or "text-before-edge" => TextBaselineKind.Hanging,
                    _ => TextBaselineKind.Alphabetic,
                },
                Opacity = opacity,
            };

        static bool IsBold(string? weight) =>
            weight != null &&
            (weight.Equals("bold", StringComparison.OrdinalIgnoreCase) ||
             weight.Equals("bolder", StringComparison.OrdinalIgnoreCase) ||
             (int.TryParse(weight, out var numeric) && numeric >= 600));

        static IReadOnlyList<string> FontFamilies(string families) =>
            families
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(_ => _.Trim('\'', '"'))
                .Where(_ => _.Length > 0)
                .ToList();

        // --- presentation attributes ------------------------------------------------------------

        static IReadOnlyList<(string, string)>? Presentation(SvgElement element) =>
            element switch
            {
                SvgRect rect => Attributes(("fill", rect.Fill), ("stroke", rect.Stroke), ("stroke-width", Num(rect.StrokeWidth))),
                SvgCircle circle => Attributes(("fill", circle.Fill), ("stroke", circle.Stroke), ("stroke-width", Num(circle.StrokeWidth))),
                SvgEllipse ellipse => Attributes(("fill", ellipse.Fill), ("stroke", ellipse.Stroke)),
                SvgLine line => Attributes(("stroke", line.Stroke), ("stroke-width", Num(line.StrokeWidth)), ("stroke-dasharray", line.StrokeDasharray)),
                SvgPolygon polygon => Attributes(("fill", polygon.Fill), ("stroke", polygon.Stroke)),
                SvgPath path => Attributes(
                    ("fill", path.Fill),
                    ("stroke", path.Stroke),
                    ("stroke-width", Num(path.StrokeWidth)),
                    ("stroke-dasharray", path.StrokeDasharray),
                    ("opacity", Num(path.Opacity))),
                SvgText text => Attributes(
                    ("fill", text.Fill),
                    ("font-size", text.FontSize is { } size ? size.ToString(CultureInfo.InvariantCulture) : null),
                    ("font-family", text.FontFamily),
                    ("font-weight", text.FontWeight),
                    ("text-anchor", text.TextAnchor),
                    ("dominant-baseline", text.DominantBaseline)),
                _ => null,
            };

        static IReadOnlyList<(string, string)>? Attributes(params (string Property, string? Value)[] pairs)
        {
            List<(string, string)>? result = null;
            foreach (var (property, value) in pairs)
            {
                if (value != null)
                {
                    (result ??= []).Add((property, value));
                }
            }

            return result;
        }

        static string? Num(double? value) =>
            value?.ToString(CultureInfo.InvariantCulture);

        static ElementMatch MatchFor(SvgElement element) =>
            new(TagName(element), element.Id, Split(element.Class));

        static string TagName(SvgElement element) =>
            element switch
            {
                SvgGroup => "g",
                SvgRect or SvgRectNoXY => "rect",
                SvgCircle => "circle",
                SvgEllipse => "ellipse",
                SvgLine => "line",
                SvgPolygon => "polygon",
                SvgPath => "path",
                SvgText => "text",
                SvgForeignObject => "foreignObject",
                _ => "",
            };

        static IReadOnlyList<string> Split(string? classes) =>
            classes?.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? [];

        static List<(string Property, string Value, bool Important)> InlineDeclarations(string? style)
        {
            var result = new List<(string, string, bool)>();
            if (string.IsNullOrWhiteSpace(style))
            {
                return result;
            }

            foreach (var piece in style.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var colon = piece.IndexOf(':');
                if (colon <= 0)
                {
                    continue;
                }

                var property = piece[..colon].Trim().ToLowerInvariant();
                var value = piece[(colon + 1)..].Trim();
                var important = false;
                var bang = value.IndexOf('!');
                if (bang >= 0)
                {
                    important = value[bang..].Contains("important", StringComparison.OrdinalIgnoreCase);
                    value = value[..bang].Trim();
                }

                if (property.Length > 0 && value.Length > 0)
                {
                    result.Add((property, value, important));
                }
            }

            return result;
        }

        static Dictionary<string, SvgMarker> BuildMarkerLookup(SvgDefs defs)
        {
            var lookup = new Dictionary<string, SvgMarker>(StringComparer.Ordinal);
            foreach (var marker in defs.Markers)
            {
                lookup[marker.Id] = marker;
            }

            return lookup;
        }
    }

    // --- geometry helpers -----------------------------------------------------------------------

    static List<SubPath> BuildRect(double x, double y, double width, double height, double rx, double ry)
    {
        if (rx <= 0 && ry <= 0)
        {
            return
            [
                new(
                [
                    new(ToF(x), ToF(y)),
                    new(ToF(x + width), ToF(y)),
                    new(ToF(x + width), ToF(y + height)),
                    new(ToF(x), ToF(y + height)),
                ], true),
            ];
        }

        rx = Math.Min(rx <= 0 ? ry : rx, width / 2);
        ry = Math.Min(ry <= 0 ? rx : ry, height / 2);
        var d = string.Create(
            CultureInfo.InvariantCulture,
            $"M{x + rx},{y} H{x + width - rx} A{rx},{ry} 0 0 1 {x + width},{y + ry} V{y + height - ry} A{rx},{ry} 0 0 1 {x + width - rx},{y + height} H{x + rx} A{rx},{ry} 0 0 1 {x},{y + height - ry} V{y + ry} A{rx},{ry} 0 0 1 {x + rx},{y} Z");
        return PathFlattener.Flatten(d);
    }

    static List<SubPath> BuildEllipse(double cx, double cy, double rx, double ry)
    {
        if (rx <= 0 || ry <= 0)
        {
            return [];
        }

        // Segment count scales with size so large circles stay smooth without over-tessellating tiny ones.
        var segments = Math.Clamp((int)Math.Ceiling(Math.Max(rx, ry) * 1.5), 24, 180);
        var points = new List<Vector2>(segments);
        for (var i = 0; i < segments; i++)
        {
            var theta = 2 * Math.PI * i / segments;
            points.Add(new((float)(cx + rx * Math.Cos(theta)), (float)(cy + ry * Math.Sin(theta))));
        }

        return [new(points, true)];
    }

    static ((float X, float Y) Min, (float X, float Y) Max) Bounds(IReadOnlyList<SubPath> subpaths)
    {
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        foreach (var subpath in subpaths)
        {
            foreach (var point in subpath.Points)
            {
                minX = Math.Min(minX, point.X);
                minY = Math.Min(minY, point.Y);
                maxX = Math.Max(maxX, point.X);
                maxY = Math.Max(maxY, point.Y);
            }
        }

        if (minX > maxX)
        {
            return ((0, 0), (0, 0));
        }

        return ((minX, minY), (maxX, maxY));
    }

    static (Vector2 Point, float Angle)? EndpointDirection(IReadOnlyList<SubPath> subpaths, bool atStart)
    {
        if (atStart)
        {
            foreach (var subpath in subpaths)
            {
                if (subpath.Points.Count >= 2)
                {
                    var point = subpath.Points[0];
                    var next = FirstDistinct(subpath.Points, 0, forward: true);
                    return (point, Angle(next - point));
                }
            }

            return null;
        }

        for (var s = subpaths.Count - 1; s >= 0; s--)
        {
            var points = subpaths[s].Points;
            if (points.Count >= 2)
            {
                var point = points[^1];
                var previous = FirstDistinct(points, points.Count - 1, forward: false);
                return (point, Angle(point - previous));
            }
        }

        return null;
    }

    static Vector2 FirstDistinct(List<Vector2> points, int from, bool forward)
    {
        var reference = points[from];
        if (forward)
        {
            for (var i = from + 1; i < points.Count; i++)
            {
                if (Vector2.DistanceSquared(points[i], reference) > 1e-6f)
                {
                    return points[i];
                }
            }

            return points[Math.Min(from + 1, points.Count - 1)];
        }

        for (var i = from - 1; i >= 0; i--)
        {
            if (Vector2.DistanceSquared(points[i], reference) > 1e-6f)
            {
                return points[i];
            }
        }

        return points[Math.Max(from - 1, 0)];
    }

    static float Angle(Vector2 direction) =>
        (float)Math.Atan2(direction.Y, direction.X);

    static (double, double, double, double) ParseMarkerViewBox(SvgMarker marker)
    {
        if (marker.ViewBox != null)
        {
            var parts = marker.ViewBox.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 4 &&
                double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x) &&
                double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y) &&
                double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var w) &&
                double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var h))
            {
                return (x, y, w, h);
            }
        }

        return (0, 0, marker.MarkerWidth, marker.MarkerHeight);
    }

    static float[]? ParseDash(string? dasharray)
    {
        if (string.IsNullOrWhiteSpace(dasharray) || dasharray.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var parts = dasharray.Split([',', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var values = new List<float>(parts.Length);
        var anyPositive = false;
        foreach (var part in parts)
        {
            if (double.TryParse(part.TrimEnd('p', 'x'), NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && value >= 0)
            {
                values.Add((float)value);
                anyPositive |= value > 0;
            }
        }

        if (!anyPositive)
        {
            return null;
        }

        // An odd count repeats to make the on/off pattern even, per SVG.
        if (values.Count % 2 == 1)
        {
            values.AddRange(values);
        }

        return [.. values];
    }

    static string? ExtractUrlId(string reference)
    {
        var start = reference.IndexOf('#');
        if (start < 0)
        {
            return null;
        }

        var end = reference.IndexOf(')', start);
        var id = end < 0 ? reference[(start + 1)..] : reference[(start + 1)..end];
        return id.Trim().Trim('"', '\'');
    }

    static float ToF(double value) =>
        (float)value;
}
