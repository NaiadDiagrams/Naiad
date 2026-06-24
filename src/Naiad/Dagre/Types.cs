namespace Naiad.Dagre;

/// <summary>An edge descriptor: {v, w, name}.</summary>
sealed record EdgeKey(string V, string W, string? Name = null);

/// <summary>Node label carrying every property the layout may read or write on a node.</summary>
sealed class NodeLabel
{
    public double Width;
    public double Height;
    public double? X;
    public double? Y;
    public int? Rank;
    public int? Order;
    public DummyKind? Dummy;
    public BorderKind? BorderType;
    public string? BorderTop;
    public string? BorderBottom;
    public List<string>? BorderLeft;
    public List<string>? BorderRight;
    // order/build-layer-graph: the single border-left id for one rank
    public string? BorderLeftId;
    // order/build-layer-graph: the single border-right id for one rank
    public string? BorderRightId;
    public int? MinRank;
    public int? MaxRank;
    public LabelPos? Labelpos;
    public EdgeLabel? EdgeLabel;
    public EdgeKey? EdgeKey;

    // Network-simplex tree node values.
    public int? Low;
    public int? Lim;
    public string? Parent;

    // Collected self-edges, stashed on the node while layout removes/reinserts them.
    public List<SelfEdge>? SelfEdges;
}

/// <summary>A self-edge captured during layout: the original edge object plus its label.</summary>
sealed class SelfEdge
{
    public required EdgeKey E;
    public required EdgeLabel Label;
}

/// <summary>Edge label carrying every property the layout may read or write on an edge.</summary>
sealed class EdgeLabel
{
    public List<Position>? Points;
    public double Width;
    public double Height;
    public int? Minlen;
    public double? Weight;
    public LabelPos Labelpos = LabelPos.Right;
    public double Labeloffset = 10;
    public int? LabelRank;
    public double? X;
    public double? Y;
    public bool? Reversed;
    public string? ForwardName;
    public bool? NestingEdge;
    public double? Cutvalue;
}

/// <summary>Graph label with layout configuration.</summary>
sealed class GraphLabel
{
    public double? Width;
    public double? Height;
    public Direction Rankdir;
    public double? NodeSeparation;
    public double? EdgeSeparation;
    public double? RankSeparation;
    public string? NestingRoot;
    public int? NodeRankFactor;
    public List<string>? DummyChains;
    // order/build-layer-graph: the synthetic root node id
    public string? Root;
}
