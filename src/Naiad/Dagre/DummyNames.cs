/// <summary>
/// Id prefixes for the dummy nodes dagre injects during layout. Centralised so the literals are
/// named and shared rather than scattered. Values are internal node ids only — the layout geometry
/// is id-string-independent, so these strings never affect rendered output.
/// </summary>
static class DummyNames
{
    public const string Root = "_root";
    public const string BorderLeft = "_bl";
    public const string BorderRight = "_br";
    public const string BorderTop = "_bt";
    public const string BorderBottom = "_bb";
    public const string EdgeProxy = "_ep";
    public const string Edge = "_d";
    public const string SelfEdge = "_se";
}
