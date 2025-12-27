using MermaidSharp.Layout;

namespace MermaidSharp.Diagrams.Class;

public class ClassRenderer : IDiagramRenderer<ClassModel>
{
    readonly ILayoutEngine _layoutEngine;

    const double ClassPadding = 10;
    const double LineHeight = 20;
    const double MinWidth = 100;
    const double SeparatorHeight = 1;

    public ClassRenderer(ILayoutEngine? layoutEngine = null)
    {
        _layoutEngine = layoutEngine ?? new DagreLayoutEngine();
    }

    public SvgDocument Render(ClassModel model, RenderOptions options)
    {
        // Convert to graph diagram for layout
        var graphModel = ConvertToGraphModel(model, options);

        // Run layout
        var layoutOptions = new LayoutOptions
        {
            Direction = model.Direction,
            NodeSeparation = 60,
            RankSeparation = 80
        };
        var layoutResult = _layoutEngine.Layout(graphModel, layoutOptions);

        // Build SVG
        var width = layoutResult.Width + options.Padding * 2;
        var height = layoutResult.Height + options.Padding * 2;

        var builder = new SvgBuilder()
            .Size(width, height)
            .AddArrowMarker("arrowhead", "#333")
            .AddArrowMarker("arrowhead-open", "#333");

        // Add relationship markers
        AddRelationshipMarkers(builder);

        // Render edges first (behind nodes)
        foreach (var relationship in model.Relationships)
        {
            var fromNode = graphModel.GetNode(relationship.FromId);
            var toNode = graphModel.GetNode(relationship.ToId);
            if (fromNode != null && toNode != null)
            {
                RenderRelationship(builder, relationship, fromNode, toNode, options);
            }
        }

        // Render class boxes
        foreach (var classDef in model.Classes)
        {
            var node = graphModel.GetNode(classDef.Id);
            if (node != null)
            {
                RenderClassBox(builder, classDef, node, options);
            }
        }

        return builder.Build();
    }

    GraphDiagramBase ConvertToGraphModel(ClassModel model, RenderOptions options)
    {
        var graph = new FlowchartModel { Direction = model.Direction };

        // Create nodes for each class
        foreach (var classDef in model.Classes)
        {
            var (width, height) = CalculateClassSize(classDef, options);
            var node = new Node
            {
                Id = classDef.Id,
                Label = classDef.Name,
                Width = width,
                Height = height
            };
            graph.AddNode(node);
        }

        // Create edges for each relationship
        foreach (var rel in model.Relationships)
        {
            var edge = new Edge
            {
                SourceId = rel.FromId,
                TargetId = rel.ToId,
                Label = rel.Label,
                Type = EdgeType.Arrow
            };
            graph.AddEdge(edge);
        }

        return graph;
    }

    (double width, double height) CalculateClassSize(ClassDefinition classDef, RenderOptions options)
    {
        // Calculate width based on longest text
        var maxTextWidth = MeasureText(classDef.Name, options.FontSize, true);

        if (classDef.Annotation.HasValue)
        {
            var annotationText = $"<<{classDef.Annotation.Value.ToString().ToLower()}>>";
            maxTextWidth = Math.Max(maxTextWidth, MeasureText(annotationText, options.FontSize - 2));
        }

        foreach (var member in classDef.Members)
        {
            var text = FormatMember(member);
            maxTextWidth = Math.Max(maxTextWidth, MeasureText(text, options.FontSize));
        }

        foreach (var method in classDef.Methods)
        {
            var text = FormatMethod(method);
            maxTextWidth = Math.Max(maxTextWidth, MeasureText(text, options.FontSize));
        }

        var width = Math.Max(MinWidth, maxTextWidth + ClassPadding * 2);

        // Calculate height
        var height = ClassPadding; // Top padding
        if (classDef.Annotation.HasValue)
            height += LineHeight;
        height += LineHeight; // Class name
        height += ClassPadding; // After name

        if (classDef.Members.Count > 0)
        {
            height += SeparatorHeight;
            height += classDef.Members.Count * LineHeight;
        }

        if (classDef.Methods.Count > 0)
        {
            height += SeparatorHeight;
            height += classDef.Methods.Count * LineHeight;
        }

        height += ClassPadding; // Bottom padding

        return (width, height);
    }

    void RenderClassBox(SvgBuilder builder, ClassDefinition classDef, Node node, RenderOptions options)
    {
        var x = node.Position.X - node.Width / 2;
        var y = node.Position.Y - node.Height / 2;
        var width = node.Width;
        var height = node.Height;

        // Background
        var fillColor = classDef.Annotation switch
        {
            ClassAnnotation.Interface => "#E8F4FD",
            ClassAnnotation.Abstract => "#FFF3E0",
            ClassAnnotation.Enumeration => "#E8F5E9",
            _ => "#FFFFDE"
        };

        builder.AddRect(x, y, width, height, rx: 0,
            fill: fillColor, stroke: "#333", strokeWidth: 1);

        var currentY = y + ClassPadding;
        var centerX = node.Position.X;

        // Annotation
        if (classDef.Annotation.HasValue)
        {
            var annotationText = $"<<{classDef.Annotation.Value.ToString().ToLower()}>>";
            builder.AddText(centerX, currentY + LineHeight / 2, annotationText,
                anchor: "middle",
                baseline: "middle",
                fontSize: $"{options.FontSize - 2}px",
                fontFamily: options.FontFamily,
                fontWeight: "normal",
                fill: "#666");
            currentY += LineHeight;
        }

        // Class name
        builder.AddText(centerX, currentY + LineHeight / 2, classDef.Name,
            anchor: "middle",
            baseline: "middle",
            fontSize: $"{options.FontSize}px",
            fontFamily: options.FontFamily,
            fontWeight: "bold");
        currentY += LineHeight + ClassPadding;

        // Members separator and list
        if (classDef.Members.Count > 0)
        {
            builder.AddLine(x, currentY, x + width, currentY, stroke: "#333", strokeWidth: 1);
            currentY += SeparatorHeight;

            foreach (var member in classDef.Members)
            {
                var memberText = FormatMember(member);
                builder.AddText(x + ClassPadding, currentY + LineHeight / 2, memberText,
                    anchor: "start",
                    baseline: "middle",
                    fontSize: $"{options.FontSize}px",
                    fontFamily: options.FontFamily);
                currentY += LineHeight;
            }
        }

        // Methods separator and list
        if (classDef.Methods.Count > 0)
        {
            builder.AddLine(x, currentY, x + width, currentY, stroke: "#333", strokeWidth: 1);
            currentY += SeparatorHeight;

            foreach (var method in classDef.Methods)
            {
                var methodText = FormatMethod(method);
                builder.AddText(x + ClassPadding, currentY + LineHeight / 2, methodText,
                    anchor: "start",
                    baseline: "middle",
                    fontSize: $"{options.FontSize}px",
                    fontFamily: options.FontFamily);
                currentY += LineHeight;
            }
        }
    }

    void RenderRelationship(SvgBuilder builder, ClassRelationship rel, Node fromNode, Node toNode, RenderOptions options)
    {
        // Calculate connection points
        var (startX, startY) = GetConnectionPoint(fromNode, toNode);
        var (endX, endY) = GetConnectionPoint(toNode, fromNode);

        var isDotted = rel.Type is RelationshipType.DependencyLeft or RelationshipType.DependencyRight or RelationshipType.Realization;
        var dashArray = isDotted ? "5,5" : null;

        var markerId = GetMarkerId(rel.Type);

        builder.AddLine(startX, startY, endX, endY,
            stroke: "#333",
            strokeWidth: 1,
            strokeDasharray: dashArray);

        // Draw the relationship marker at the end
        DrawRelationshipMarker(builder, rel.Type, endX, endY, startX, startY);

        // Draw label if present
        if (!string.IsNullOrEmpty(rel.Label))
        {
            var labelX = (startX + endX) / 2;
            var labelY = (startY + endY) / 2 - 10;
            builder.AddText(labelX, labelY, rel.Label,
                anchor: "middle",
                baseline: "bottom",
                fontSize: $"{options.FontSize - 2}px",
                fontFamily: options.FontFamily);
        }

        // Draw cardinalities
        if (!string.IsNullOrEmpty(rel.FromCardinality))
        {
            builder.AddText(startX + 10, startY - 10, rel.FromCardinality,
                anchor: "start",
                baseline: "bottom",
                fontSize: $"{options.FontSize - 2}px",
                fontFamily: options.FontFamily);
        }
        if (!string.IsNullOrEmpty(rel.ToCardinality))
        {
            builder.AddText(endX - 10, endY - 10, rel.ToCardinality,
                anchor: "end",
                baseline: "bottom",
                fontSize: $"{options.FontSize - 2}px",
                fontFamily: options.FontFamily);
        }
    }

    (double x, double y) GetConnectionPoint(Node from, Node to)
    {
        // Calculate the point on the edge of 'from' node facing 'to' node
        var dx = to.Position.X - from.Position.X;
        var dy = to.Position.Y - from.Position.Y;

        // Determine which edge to use based on direction
        if (Math.Abs(dx) > Math.Abs(dy))
        {
            // Horizontal connection
            return dx > 0
                ? (from.Position.X + from.Width / 2, from.Position.Y)
                : (from.Position.X - from.Width / 2, from.Position.Y);
        }
        else
        {
            // Vertical connection
            return dy > 0
                ? (from.Position.X, from.Position.Y + from.Height / 2)
                : (from.Position.X, from.Position.Y - from.Height / 2);
        }
    }

    void DrawRelationshipMarker(SvgBuilder builder, RelationshipType type, double x, double y, double fromX, double fromY)
    {
        var angle = Math.Atan2(y - fromY, x - fromX);
        var markerSize = 10.0;

        switch (type)
        {
            case RelationshipType.Inheritance:
            case RelationshipType.Realization:
                // Hollow triangle
                var points = GetTrianglePoints(x, y, angle, markerSize);
                builder.AddPolygon(points, fill: "#fff", stroke: "#333");
                break;

            case RelationshipType.Composition:
                // Filled diamond
                var diamondPoints = GetDiamondPoints(x, y, angle, markerSize);
                builder.AddPolygon(diamondPoints, fill: "#333", stroke: "#333");
                break;

            case RelationshipType.Aggregation:
                // Hollow diamond
                var aggDiamondPoints = GetDiamondPoints(x, y, angle, markerSize);
                builder.AddPolygon(aggDiamondPoints, fill: "#fff", stroke: "#333");
                break;

            case RelationshipType.Association:
            case RelationshipType.DependencyLeft:
            case RelationshipType.DependencyRight:
                // Arrow
                var arrowPoints = GetArrowPoints(x, y, angle, markerSize);
                builder.AddPolygon(arrowPoints, fill: "#333");
                break;
        }
    }

    Position[] GetTrianglePoints(double x, double y, double angle, double size)
    {
        var backAngle1 = angle + Math.PI - Math.PI / 6;
        var backAngle2 = angle + Math.PI + Math.PI / 6;

        return
        [
            new Position(x, y),
            new Position(x + size * Math.Cos(backAngle1), y + size * Math.Sin(backAngle1)),
            new Position(x + size * Math.Cos(backAngle2), y + size * Math.Sin(backAngle2))
        ];
    }

    Position[] GetDiamondPoints(double x, double y, double angle, double size)
    {
        var halfSize = size / 2;
        return
        [
            new Position(x, y),
            new Position(x - halfSize * Math.Cos(angle) + halfSize * Math.Sin(angle),
                        y - halfSize * Math.Sin(angle) - halfSize * Math.Cos(angle)),
            new Position(x - size * Math.Cos(angle), y - size * Math.Sin(angle)),
            new Position(x - halfSize * Math.Cos(angle) - halfSize * Math.Sin(angle),
                        y - halfSize * Math.Sin(angle) + halfSize * Math.Cos(angle))
        ];
    }

    Position[] GetArrowPoints(double x, double y, double angle, double size)
    {
        var halfSize = size * 0.4;
        var backAngle1 = angle + Math.PI - Math.PI / 6;
        var backAngle2 = angle + Math.PI + Math.PI / 6;

        return
        [
            new Position(x, y),
            new Position(x + size * Math.Cos(backAngle1), y + size * Math.Sin(backAngle1)),
            new Position(x + size * Math.Cos(backAngle2), y + size * Math.Sin(backAngle2))
        ];
    }

    static void AddRelationshipMarkers(SvgBuilder builder)
    {
        // Inheritance marker (hollow triangle)
        builder.AddMarker("inheritance", "M0,0 L10,5 L0,10 Z", 12, 12, 10, 5, "#fff");
        // Composition marker (filled diamond)
        builder.AddMarker("composition", "M0,5 L5,0 L10,5 L5,10 Z", 12, 12, 10, 5, "#333");
        // Aggregation marker (hollow diamond)
        builder.AddMarker("aggregation", "M0,5 L5,0 L10,5 L5,10 Z", 12, 12, 10, 5, "#fff");
    }

    static string GetMarkerId(RelationshipType type) =>
        type switch
        {
            RelationshipType.Inheritance or RelationshipType.Realization => "inheritance",
            RelationshipType.Composition => "composition",
            RelationshipType.Aggregation => "aggregation",
            _ => "arrowhead"
        };

    static string FormatMember(ClassMember member)
    {
        var visibility = GetVisibilitySymbol(member.Visibility);
        var staticPrefix = member.IsStatic ? "$ " : "";
        var typeStr = !string.IsNullOrEmpty(member.Type) ? $" : {member.Type}" : "";
        return $"{visibility}{staticPrefix}{member.Name}{typeStr}";
    }

    static string FormatMethod(ClassMethod method)
    {
        var visibility = GetVisibilitySymbol(method.Visibility);
        var staticPrefix = method.IsStatic ? "$ " : "";
        var abstractPrefix = method.IsAbstract ? "* " : "";
        var returnTypeStr = !string.IsNullOrEmpty(method.ReturnType) ? $" : {method.ReturnType}" : "";
        return $"{visibility}{staticPrefix}{abstractPrefix}{method.Name}(){returnTypeStr}";
    }

    static string GetVisibilitySymbol(Visibility visibility) =>
        visibility switch
        {
            Visibility.Public => "+ ",
            Visibility.Private => "- ",
            Visibility.Protected => "# ",
            Visibility.PackagePrivate => "~ ",
            _ => ""
        };

    static double MeasureText(string text, double fontSize, bool bold = false)
    {
        var factor = bold ? 0.65 : 0.55;
        return text.Length * fontSize * factor;
    }

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}

// Temporary model for layout - reusing flowchart structure
file class FlowchartModel : GraphDiagramBase { }
