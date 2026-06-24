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