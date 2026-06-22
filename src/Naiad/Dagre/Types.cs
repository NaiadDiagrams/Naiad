namespace Naiad.Dagre;

/// <summary>An edge descriptor: {v, w, name}.</summary>
sealed record Edge(string V, string W, string? Name = null);

struct Point(double x, double y)
{
    public double X = x;
    public double Y = y;
}

sealed class PartitionResult<T>
{
    public List<T> Lhs = [];
    public List<T> Rhs = [];
}

/// <summary>Node label carrying every property the layout may read or write on a node.</summary>
sealed class NodeLabel
{
    public double Width;
    public double Height;
    public double? X;
    public double? Y;
    public int? Rank;
    public int? Order;
    public string? Dummy;            // 'edge' | 'border' | 'edge-label' | 'edge-proxy' | 'selfedge' | 'root'
    public string? BorderType;       // 'borderLeft' | 'borderRight'
    public string? BorderTop;
    public string? BorderBottom;
    public List<string>? BorderLeft;
    public List<string>? BorderRight;
    public string? BorderLeftId;     // order/build-layer-graph: the single border-left id for one rank
    public string? BorderRightId;    // order/build-layer-graph: the single border-right id for one rank
    public int? MinRank;
    public int? MaxRank;
    public string? Label;
    public string? Labelpos;         // 'l' | 'c' | 'r'
    public EdgeLabel? EdgeLabel;
    public Edge? EdgeObj;

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
    public required Edge E;
    public required EdgeLabel Label;
}

/// <summary>Edge label carrying every property the layout may read or write on an edge.</summary>
sealed class EdgeLabel
{
    public List<Point>? Points;
    public double? Width;
    public double? Height;
    public int? Minlen;
    public double? Weight;
    public string? Labelpos;         // 'l' | 'c' | 'r'
    public double? Labeloffset;
    public int? LabelRank;
    public double? X;
    public double? Y;
    public double? E;
    public bool? Reversed;
    public string? ForwardName;
    public bool? NestingEdge;
    public double? Cutvalue;
    public Edge? EdgeObj;
}

/// <summary>Graph label with layout configuration.</summary>
sealed class GraphLabel
{
    public double? Width;
    public double? Height;
    public Direction Rankdir;
    public string? Align;            // 'UL' | 'UR' | 'DL' | 'DR'
    public double? Nodesep;
    public double? Edgesep;
    public double? Ranksep;
    public double? Marginx;
    public double? Marginy;
    public string? Acyclicer;        // 'greedy'
    public string? Ranker;           // 'network-simplex' | 'tight-tree' | 'longest-path'
    public string? Rankalign;        // 'top' | 'center' | 'bottom'
    public string? NestingRoot;
    public int? NodeRankFactor;
    public List<string>? DummyChains;
    public string? Root;             // order/build-layer-graph: the synthetic root node id
    public int? MaxRank;             // largest subgraph border rank
}
