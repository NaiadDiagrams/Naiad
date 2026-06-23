namespace Naiad.Dagre;

/// <summary>
/// A directed, optionally multigraph and/or compound graph, specialized to the layout label types.
/// Node/edge insertion order is preserved because the layout's output depends on iteration order.
/// </summary>
sealed class Graph
{
    const string defaultEdgeName = "\x00";
    internal const string GraphNode = "\x00";
    const string edgeKeyDelim = "\x01";

    GraphLabel? label;
    readonly OrderedMap<NodeLabel> nodesMap = new();
    readonly OrderedMap<OrderedMap<Edge>> inMap = new();      // v -> edgeId -> edgeObj
    readonly OrderedMap<OrderedMap<int>> predsMap = new();    // u -> v -> count
    readonly OrderedMap<OrderedMap<Edge>> outMap = new();     // v -> edgeId -> edgeObj
    readonly OrderedMap<OrderedMap<int>> sucsMap = new();     // v -> w -> count
    readonly OrderedMap<Edge> edgeObjsMap = new();            // edgeId -> edgeObj
    readonly OrderedMap<EdgeLabel> edgeLabelsMap = new();     // edgeId -> label
    int uniqueIdCounter;
    readonly OrderedMap<string>? parentMap;
    readonly OrderedMap<OrderedMap<bool>>? childrenMap;

    Func<string, NodeLabel> defaultNodeLabelFn = _ => null!;
    Func<string, string, string?, EdgeLabel> defaultEdgeLabelFn = (_, _, _) => null!;

    public Graph(bool directed = true, bool multigraph = false, bool compound = false)
    {
        IsDirected = directed;
        IsMultigraph = multigraph;
        IsCompound = compound;

        if (IsCompound)
        {
            parentMap = new();
            childrenMap = new()
            {
                [GraphNode] = new()
            };
        }
    }

    public bool IsDirected { get; }

    public bool IsMultigraph { get; }

    public bool IsCompound { get; }

    /// <summary>
    /// Returns an id with the given prefix that is unique within this graph, from a per-graph counter.
    /// A per-graph counter (rather than a shared static) keeps every layout reproducible by construction.
    /// Callers still guard against pre-existing nodes via HasNode.
    /// </summary>
    public string UniqueId(string prefix) =>
        prefix + (++uniqueIdCounter).ToString(CultureInfo.InvariantCulture);

    /* === Graph functions ========= */

    public void SetGraph(GraphLabel? value) => label = value;

    public GraphLabel Label => label!;

    public Graph SetDefaultNodeLabel(Func<string, NodeLabel> fn)
    {
        defaultNodeLabelFn = fn;
        return this;
    }

    public int NodeCount { get; private set; }

    public List<string> Nodes() => nodesMap.Keys();

    /// <summary>The nodes paired with their labels, in insertion order — a snapshot (safe to remove nodes
    /// while iterating), avoiding the per-node <see cref="NodeLabel"/> lookup that <see cref="Nodes"/> forces.</summary>
    public List<KeyValuePair<string, NodeLabel>> NodeEntries() => nodesMap.Entries();

    /// <summary>The node labels in insertion order — the labels of <see cref="Nodes"/> without the per-node
    /// lookup. A snapshot (safe to mutate the graph while iterating), mirroring <see cref="EdgeLabels"/>.</summary>
    public List<NodeLabel> NodeLabels() => nodesMap.Values();

    public List<string> Sources() => Nodes().Where(v => inMap[v].Count == 0).ToList();

    public Graph SetNode(string name)
    {
        if (nodesMap.ContainsKey(name))
        {
            return this;
        }

        nodesMap[name] = defaultNodeLabelFn(name);
        InitNode(name);
        return this;
    }

    public Graph SetNode(string name, NodeLabel value)
    {
        if (nodesMap.ContainsKey(name))
        {
            nodesMap[name] = value;
            return this;
        }

        nodesMap[name] = value;
        InitNode(name);
        return this;
    }

    void InitNode(string name)
    {
        if (IsCompound)
        {
            parentMap![name] = GraphNode;
            childrenMap![name] = new();
            childrenMap[GraphNode][name] = true;
        }

        inMap[name] = new();
        predsMap[name] = new();
        outMap[name] = new();
        sucsMap[name] = new();
        NodeCount++;
    }

    public NodeLabel NodeLabel(string name) =>
        nodesMap.GetValueOrDefault(name) ?? throw new KeyNotFoundException($"Graph has no node label for '{name}'");

    public bool TryGetNodeLabel(string name, out NodeLabel label)
    {
        var found = nodesMap.GetValueOrDefault(name);
        label = found!;
        return found != null;
    }

    public bool HasNode(string name) => nodesMap.ContainsKey(name);

    public Graph RemoveNode(string name)
    {
        if (nodesMap.ContainsKey(name))
        {
            nodesMap.Remove(name);
            if (IsCompound)
            {
                RemoveFromParentsChildList(name);
                parentMap!.Remove(name);
                foreach (var child in Children(name))
                {
                    SetParent(child);
                }

                childrenMap!.Remove(name);
            }

            foreach (var e in inMap[name].Keys())
            {
                RemoveEdge(edgeObjsMap[e]);
            }

            inMap.Remove(name);
            predsMap.Remove(name);
            foreach (var e in outMap[name].Keys())
            {
                RemoveEdge(edgeObjsMap[e]);
            }

            outMap.Remove(name);
            sucsMap.Remove(name);
            NodeCount--;
        }

        return this;
    }

    public Graph SetParent(string v, string? parent = null)
    {
        if (!IsCompound)
        {
            throw new InvalidOperationException("Cannot set parent in a non-compound graph");
        }

        if (parent == null)
        {
            parent = GraphNode;
        }
        else
        {
            for (var ancestor = parent; ancestor != null; ancestor = Parent(ancestor))
            {
                if (ancestor == v)
                {
                    throw new InvalidOperationException($"Setting {parent} as parent of {v} would create a cycle");
                }
            }

            SetNode(parent);
        }

        SetNode(v);
        RemoveFromParentsChildList(v);
        parentMap![v] = parent;
        childrenMap![parent][v] = true;
        return this;
    }

    public string? Parent(string v)
    {
        if (IsCompound)
        {
            var parent = parentMap!.GetValueOrDefault(v);
            if (parent != GraphNode)
            {
                return parent;
            }
        }

        return null;
    }

    public List<string> Children(string v = GraphNode)
    {
        if (IsCompound)
        {
            if (childrenMap!.TryGetValue(v, out var children))
            {
                return children.Keys();
            }
        }
        else if (v == GraphNode)
        {
            return Nodes();
        }

        return [];
    }

    // The number of children of v without materialising the key list that Children allocates — for the hot
    // leaf checks (SortSubgraph, AsNonCompoundGraph, InitOrder, copy-out) that only need the count.
    public int ChildCount(string v = GraphNode)
    {
        if (IsCompound)
        {
            return childrenMap!.TryGetValue(v, out var children) ? children.Count : 0;
        }

        return v == GraphNode ? NodeCount : 0;
    }

    public List<string>? Predecessors(string v) =>
        predsMap.TryGetValue(v, out var predsV) ? predsV.Keys() : null;

    public List<string>? Successors(string v) =>
        sucsMap.TryGetValue(v, out var sucsV) ? sucsV.Keys() : null;

    // Allocation-free neighbour/edge views for the hot order/position passes. The node must exist
    // (those passes only ever pass real nodes), so unlike Successors/OutEdges these never return null
    // and must not be used while the graph is being mutated.
    public OrderedMap<int>.KeyEnumerable SuccessorsOf(string v) => sucsMap[v].EnumerateKeys();

    public OrderedMap<int>.KeyEnumerable PredecessorsOf(string v) => predsMap[v].EnumerateKeys();

    public OrderedMap<Edge>.ValueEnumerable InEdgesOf(string v) => inMap[v].EnumerateValues();

    public OrderedMap<Edge>.ValueEnumerable OutEdgesOf(string v) => outMap[v].EnumerateValues();

    public List<string>? Neighbors(string v)
    {
        var preds = Predecessors(v);
        if (preds != null)
        {
            var union = new List<string>(preds);
            var seen = new HashSet<string>(preds, StringComparer.Ordinal);
            foreach (var succ in Successors(v)!)
            {
                if (seen.Add(succ))
                {
                    union.Add(succ);
                }
            }

            return union;
        }

        return null;
    }

    public Graph SetDefaultEdgeLabel(Func<string, string, string?, EdgeLabel> fn)
    {
        defaultEdgeLabelFn = fn;
        return this;
    }

    public Graph SetDefaultEdgeLabel(EdgeLabel value)
    {
        defaultEdgeLabelFn = (_, _, _) => value;
        return this;
    }

    public List<Edge> Edges() => edgeObjsMap.Values();

    // Allocation-free enumeration of all edges — the network-simplex pivot loop scans these per iteration.
    public OrderedMap<Edge>.ValueEnumerable EnumerateEdges() => edgeObjsMap.EnumerateValues();

    /// <summary>The edge labels in insertion order — the labels of <see cref="Edges"/>, without the per-edge lookup.</summary>
    public List<EdgeLabel> EdgeLabels() => edgeLabelsMap.Values();

    public Graph SetEdge(string v, string w) => SetEdgeCore(v, w, null, null, false);

    public Graph SetEdge(string v, string w, EdgeLabel? value) => SetEdgeCore(v, w, null, value, true);

    public Graph SetEdge(string v, string w, EdgeLabel? value, string? name) => SetEdgeCore(v, w, name, value, true);

    public Graph SetEdge(Edge edge, EdgeLabel? value) => SetEdgeCore(edge.V, edge.W, edge.Name, value, true);

    Graph SetEdgeCore(string v, string w, string? name, EdgeLabel? value, bool valueSpecified)
    {
        var e = EdgeArgsToId(IsDirected, v, w, name);
        if (edgeLabelsMap.ContainsKey(e))
        {
            if (valueSpecified)
            {
                edgeLabelsMap[e] = value!;
            }

            return this;
        }

        if (name != null && !IsMultigraph)
        {
            throw new InvalidOperationException("Cannot set a named edge when isMultigraph = false");
        }

        SetNode(v);
        SetNode(w);

        edgeLabelsMap[e] = valueSpecified ? value! : defaultEdgeLabelFn(v, w, name);

        var edgeObj = EdgeArgsToObj(IsDirected, v, w, name);
        var vStr = edgeObj.V;
        var wStr = edgeObj.W;

        edgeObjsMap[e] = edgeObj;
        IncrementOrInitEntry(predsMap[wStr], vStr);
        IncrementOrInitEntry(sucsMap[vStr], wStr);
        inMap[wStr][e] = edgeObj;
        outMap[vStr][e] = edgeObj;
        return this;
    }

    public EdgeLabel FindEdgeLabel(string v, string w, string? name = null)
    {
        if (edgeLabelsMap.TryGetValue(EdgeArgsToId(IsDirected, v, w, name), out var label))
        {
            return label;
        }

        throw new KeyNotFoundException($"Graph has no edge {DescribeEdge(v, w, name)}");
    }

    public EdgeLabel FindEdgeLabel(Edge edge)
    {
        if (edgeLabelsMap.TryGetValue(EdgeObjToId(IsDirected, edge), out var label))
        {
            return label;
        }

        throw new KeyNotFoundException($"Graph has no edge {DescribeEdge(edge.V, edge.W, edge.Name)}");
    }

    public bool TryGetEdgeLabel(string v, string w, out EdgeLabel label) =>
        edgeLabelsMap.TryGetValue(EdgeArgsToId(IsDirected, v, w, null), out label);

    static string DescribeEdge(string v, string w, string? name)
    {
        if (name == null)
        {
            return $"({v}, {w})";
        }

        return $"({v}, {w}, {name})";
    }

    public bool HasEdge(string v, string w, string? name = null) =>
        edgeLabelsMap.ContainsKey(EdgeArgsToId(IsDirected, v, w, name));

    public void RemoveEdge(string v, string w, string? name = null) => RemoveEdgeId(EdgeArgsToId(IsDirected, v, w, name));

    public void RemoveEdge(Edge edge) => RemoveEdgeId(EdgeObjToId(IsDirected, edge));

    void RemoveEdgeId(string e)
    {
        if (!edgeObjsMap.TryGetValue(e, out var edge))
        {
            return;
        }

        var vStr = edge.V;
        var wStr = edge.W;
        edgeLabelsMap.Remove(e);
        edgeObjsMap.Remove(e);
        DecrementOrRemoveEntry(predsMap[wStr], vStr);
        DecrementOrRemoveEntry(sucsMap[vStr], wStr);
        inMap[wStr].Remove(e);
        outMap[vStr].Remove(e);
    }

    public List<Edge>? NodeEdges(string v)
    {
        if (nodesMap.ContainsKey(v))
        {
            var combined = new OrderedMap<Edge>();
            foreach (var kv in inMap[v])
            {
                combined[kv.Key] = kv.Value;
            }

            foreach (var kv in outMap[v])
            {
                combined[kv.Key] = kv.Value;
            }

            return combined.Values();
        }

        return null;
    }

    void RemoveFromParentsChildList(string v) => childrenMap![parentMap![v]].Remove(v);

    static void IncrementOrInitEntry(OrderedMap<int> map, string k)
    {
        if (map.TryGetValue(k, out var count) && count != 0)
        {
            map[k] = count + 1;
        }
        else
        {
            map[k] = 1;
        }
    }

    static void DecrementOrRemoveEntry(OrderedMap<int> map, string k)
    {
        if (!map.TryGetValue(k, out var count))
        {
            return;
        }

        count--;
        if (count == 0)
        {
            map.Remove(k);
        }
        else
        {
            map[k] = count;
        }
    }

    static string EdgeArgsToId(bool isDirected, string vIn, string wIn, string? name)
    {
        var v = vIn;
        var w = wIn;
        if (!isDirected && string.CompareOrdinal(v, w) > 0)
        {
            (v, w) = (w, v);
        }

        return v + edgeKeyDelim + w + edgeKeyDelim + (name ?? defaultEdgeName);
    }

    static Edge EdgeArgsToObj(bool isDirected, string vIn, string wIn, string? name)
    {
        var v = vIn;
        var w = wIn;
        if (!isDirected && string.CompareOrdinal(v, w) > 0)
        {
            (v, w) = (w, v);
        }

        return new(v, w, string.IsNullOrEmpty(name) ? null : name);
    }

    static string EdgeObjToId(bool isDirected, Edge edgeObj) =>
        EdgeArgsToId(isDirected, edgeObj.V, edgeObj.W, edgeObj.Name);
}
