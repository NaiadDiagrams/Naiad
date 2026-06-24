/// <summary>A dummy node's role in the layout. A real (non-dummy) node has a null <c>Dummy</c>.</summary>
enum DummyKind
{
    Edge,
    Border,
    EdgeLabel,
    EdgeProxy,
    SelfEdge,
    Root
}

/// <summary>Which side a subgraph's border segment runs down.</summary>
enum BorderKind
{
    Left,
    Right
}

/// <summary>An edge label's position relative to its edge.</summary>
enum LabelPos
{
    Left,
    Center,
    Right
}
