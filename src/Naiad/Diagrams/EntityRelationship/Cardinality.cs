namespace MermaidSharp.Diagrams.EntityRelationship;

public enum Cardinality
{
    ExactlyOne,   // ||
    ZeroOrOne,    // |o or o|
    OneOrMore,    // }| or |{
    ZeroOrMore    // }o or o{
}