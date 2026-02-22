namespace MermaidSharp.Diagrams.Class;

public enum RelationshipType
{
    Inheritance,      // <|--
    Composition,      // *--
    Aggregation,      // o--
    Association,      // -->
    DependencyLeft,   // ..>
    DependencyRight,  // <..
    Realization,      // ..|>
    Link              // --
}