namespace Naiad.Diagrams.Class;

/// <summary>
/// The UML kind of a relationship, classified from the markers on its two ends. Rendering is driven by
/// <see cref="ClassRelationship.FromMarker"/> and <see cref="ClassRelationship.ToMarker"/>, which record
/// which end each glyph belongs on; this is the semantic summary for callers inspecting the model.
/// </summary>
public enum RelationshipType
{
    Inheritance,      // <|-- or --|>
    Composition,      // *-- or --*
    Aggregation,      // o-- or --o
    Association,      // --> or <--
    DependencyLeft,   // <..
    DependencyRight,  // ..>
    Realization,      // <|.. or ..|>
    Link              // -- or ..
}
