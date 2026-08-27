namespace Naiad.Diagrams.Class;

public class ClassRelationship
{
    public required string FromId { get; init; }
    public required string ToId { get; init; }
    public RelationshipType Type { get; set; } = RelationshipType.Association;

    /// <summary>Marker drawn at the <see cref="FromId"/> end, e.g. the triangle of <c>Animal &lt;|-- Dog</c>.</summary>
    public RelationshipMarker FromMarker { get; set; }

    /// <summary>Marker drawn at the <see cref="ToId"/> end, e.g. the arrowhead of <c>Student --&gt; Course</c>.</summary>
    public RelationshipMarker ToMarker { get; set; }

    /// <summary>True for the <c>..</c> line forms (dependency and realization), which render dashed.</summary>
    public bool IsDashed { get; set; }

    public string? Label { get; set; }
    public string? FromCardinality { get; set; }
    public string? ToCardinality { get; set; }
}
