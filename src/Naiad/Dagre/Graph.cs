namespace Naiad.Dagre;

/// <summary>
/// A faithful C# port of <c>@dagrejs/graphlib</c>'s <c>Graph</c> (a directed, optionally multigraph and/or
/// compound graph), specialized to the dagre label types. Node/edge insertion order is preserved exactly as
/// the JavaScript object implementation does, because dagre's output depends on iteration order.
/// </summary>
sealed class Graph
{
    internal const string DefaultEdgeName = "\x00";
    internal const string GraphNode = "\x00";
    const string EdgeKeyDelim = "\x01";

    readonly bool isDirected;
    readonly bool isMultigraph;
    readonly bool isCompound;

    GraphLabel? label;
    readonly OrderedMap<NodeLabel> nodesMap = new();
    readonly OrderedMap<OrderedMap<Edge>> inMap = new();      // v -> edgeId -> edgeObj
    readonly OrderedMap<OrderedMap<int>> predsMap = new();    // u -> v -> count
    readonly OrderedMap<OrderedMap<Edge>> outMap = new();     // v -> edgeId -> edgeObj
    readonly OrderedMap<OrderedMap<int>> sucsMap = new();     // v -> w -> count
    readonly OrderedMap<Edge> edgeObjsMap = new();            // edgeId -> edgeObj
    readonly OrderedMap<EdgeLabel> edgeLabelsMap = new();     // edgeId -> label
    int nodeCountValue;
    int edgeCountValue;
    int uniqueIdCounter;
    readonly OrderedMap<string>? parentMap;
    readonly OrderedMap<OrderedMap<bool>>? childrenMap;

    Func<string, NodeLabel> defaultNodeLabelFn = _ => null!;
    Func<string, string, string?, EdgeLabel> defaultEdgeLabelFn = (_, _, _) => null!;

    public Graph(bool directed = true, bool multigraph = false, bool compound = false)
    {
        isDirected = directed;
        isMultigraph = multigraph;
        isCompound = compound;

        if (isCompound)
        {
            parentMap = new();
            childrenMap = new()
            {
                [GraphNode] = new()
            };
        }
    }

    public bool IsDirected() => isDirected;

    public bool IsMultigraph() => isMultigraph;

    public bool IsCompound() => isCompound;

    /// <summary>
    /// Returns an id with the given prefix that is unique within this graph, from a per-graph counter.
    /// dagre uses a module-global counter; a per-graph one removes shared mutable state and makes every
    /// layout reproducible by construction. Callers still guard against pre-existing nodes via HasNode.
    /// </summary>
    public string UniqueId(string prefix) =>
        prefix + (++uniqueIdCounter).ToString(CultureInfo.InvariantCulture);

    /* === Graph functions ========= */

    public Graph SetGraph(GraphLabel? value)
    {
        label = value;
        return this;
    }

    public GraphLabel Graph_() => label!;

    public Graph SetDefaultNodeLabel(Func<string, NodeLabel> fn)
    {
        defaultNodeLabelFn = fn;
        return this;
    }

    public Graph SetDefaultNodeLabel(NodeLabel value)
    {
        defaultNodeLabelFn = _ => value;
        return this;
    }

    public int NodeCount() => nodeCountValue;

    /* === Node functions ========== */

    public List<string> Nodes() => nodesMap.Keys();

    public List<string> Sources() => Nodes().Where(v => inMap[v].Count == 0).ToList();

    public List<string> Sinks() => Nodes().Where(v => outMap[v].Count == 0).ToList();

    public Graph SetNodes(IEnumerable<string> names, NodeLabel? value = null)
    {
        foreach (var v in names)
        {
            if (value != null)
            {
                SetNode(v, value);
            }
            else
            {
                SetNode(v);
            }
        }

        return this;
    }

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
        if (isCompound)
        {
            parentMap![name] = GraphNode;
            childrenMap![name] = new();
            childrenMap[GraphNode][name] = true;
        }

        inMap[name] = new();
        predsMap[name] = new();
        outMap[name] = new();
        sucsMap[name] = new();
        ++nodeCountValue;
    }

    public NodeLabel Node(string name) => nodesMap.GetValueOrDefault(name)!;

    public bool HasNode(string name) => nodesMap.ContainsKey(name);

    public Graph RemoveNode(string name)
    {
        if (nodesMap.ContainsKey(name))
        {
            nodesMap.Remove(name);
            if (isCompound)
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
            --nodeCountValue;
        }

        return this;
    }

    public Graph SetParent(string v, string? parent = null)
    {
        if (!isCompound)
        {
            throw new InvalidOperationException("Cannot set parent in a non-compound graph");
        }

        if (parent == null)
        {
            parent = GraphNode;
        }
        else
        {
            for (string? ancestor = parent; ancestor != null; ancestor = Parent(ancestor))
            {
                if (ancestor == v)
                {
                    throw new InvalidOperationException("Setting " + parent + " as parent of " + v + " would create a cycle");
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
        if (isCompound)
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
        if (isCompound)
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

    // Child count without materialising the key-snapshot list that Children(v).Count would allocate.
    public int ChildCount(string v = GraphNode)
    {
        if (isCompound)
        {
            return childrenMap!.TryGetValue(v, out var children) ? children.Count : 0;
        }

        return v == GraphNode ? nodeCountValue : 0;
    }

    public List<string>? Predecessors(string v) =>
        predsMap.TryGetValue(v, out var predsV) ? predsV.Keys() : null;

    public List<string>? Successors(string v) =>
        sucsMap.TryGetValue(v, out var sucsV) ? sucsV.Keys() : null;

    // Allocation-free neighbour/edge views for the hot order/position passes. The node must exist
    // (those passes only ever pass real nodes), so unlike Successors/InEdges these never return null
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

    public bool IsLeaf(string v)
    {
        var neighbors = isDirected ? Successors(v) : Neighbors(v);
        return neighbors!.Count == 0;
    }

    public Graph FilterNodes(Func<string, bool> filter)
    {
        var copy = new Graph(isDirected, isMultigraph, isCompound);
        copy.SetGraph(Graph_());

        foreach (var v in nodesMap.Keys())
        {
            if (filter(v))
            {
                copy.SetNode(v, nodesMap[v]);
            }
        }

        foreach (var e in edgeObjsMap.Values())
        {
            if (copy.HasNode(e.V) && copy.HasNode(e.W))
            {
                copy.SetEdge(e, Edge_(e));
            }
        }

        var parents = new Dictionary<string, string?>(StringComparer.Ordinal);

        string? FindParent(string v)
        {
            var parent = Parent(v);
            if (parent == null || copy.HasNode(parent))
            {
                parents[v] = parent;
                return parent;
            }

            if (parents.TryGetValue(parent, out var cached))
            {
                return cached;
            }

            return FindParent(parent);
        }

        if (isCompound)
        {
            foreach (var v in copy.Nodes())
            {
                copy.SetParent(v, FindParent(v));
            }
        }

        return copy;
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

    public int EdgeCount() => edgeCountValue;

    public List<Edge> Edges() => edgeObjsMap.Values();

    public Graph SetPath(IReadOnlyList<string> nodes, EdgeLabel? value = null)
    {
        for (var i = 0; i + 1 < nodes.Count; i++)
        {
            if (value != null)
            {
                SetEdge(nodes[i], nodes[i + 1], value);
            }
            else
            {
                SetEdge(nodes[i], nodes[i + 1]);
            }
        }

        return this;
    }

    public Graph SetEdge(string v, string w) => SetEdgeCore(v, w, null, null, false);

    public Graph SetEdge(string v, string w, EdgeLabel? value) => SetEdgeCore(v, w, null, value, true);

    public Graph SetEdge(string v, string w, EdgeLabel? value, string? name) => SetEdgeCore(v, w, name, value, true);

    public Graph SetEdge(Edge edge) => SetEdgeCore(edge.V, edge.W, edge.Name, null, false);

    public Graph SetEdge(Edge edge, EdgeLabel? value) => SetEdgeCore(edge.V, edge.W, edge.Name, value, true);

    Graph SetEdgeCore(string v, string w, string? name, EdgeLabel? value, bool valueSpecified)
    {
        var e = EdgeArgsToId(isDirected, v, w, name);
        if (edgeLabelsMap.ContainsKey(e))
        {
            if (valueSpecified)
            {
                edgeLabelsMap[e] = value!;
            }

            return this;
        }

        if (name != null && !isMultigraph)
        {
            throw new InvalidOperationException("Cannot set a named edge when isMultigraph = false");
        }

        SetNode(v);
        SetNode(w);

        edgeLabelsMap[e] = valueSpecified ? value! : defaultEdgeLabelFn(v, w, name);

        var edgeObj = EdgeArgsToObj(isDirected, v, w, name);
        var vStr = edgeObj.V;
        var wStr = edgeObj.W;

        edgeObjsMap[e] = edgeObj;
        IncrementOrInitEntry(predsMap[wStr], vStr);
        IncrementOrInitEntry(sucsMap[vStr], wStr);
        inMap[wStr][e] = edgeObj;
        outMap[vStr][e] = edgeObj;
        edgeCountValue++;
        return this;
    }

    public EdgeLabel Edge_(string v, string w, string? name = null) =>
        edgeLabelsMap.GetValueOrDefault(EdgeArgsToId(isDirected, v, w, name))!;

    public EdgeLabel Edge_(Edge edge) =>
        edgeLabelsMap.GetValueOrDefault(EdgeObjToId(isDirected, edge))!;

    public bool HasEdge(string v, string w, string? name = null) =>
        edgeLabelsMap.ContainsKey(EdgeArgsToId(isDirected, v, w, name));

    public bool HasEdge(Edge edge) =>
        edgeLabelsMap.ContainsKey(EdgeObjToId(isDirected, edge));

    public Graph RemoveEdge(string v, string w, string? name = null) => RemoveEdgeId(EdgeArgsToId(isDirected, v, w, name));

    public Graph RemoveEdge(Edge edge) => RemoveEdgeId(EdgeObjToId(isDirected, edge));

    Graph RemoveEdgeId(string e)
    {
        if (edgeObjsMap.TryGetValue(e, out var edge))
        {
            var vStr = edge.V;
            var wStr = edge.W;
            edgeLabelsMap.Remove(e);
            edgeObjsMap.Remove(e);
            DecrementOrRemoveEntry(predsMap[wStr], vStr);
            DecrementOrRemoveEntry(sucsMap[vStr], wStr);
            inMap[wStr].Remove(e);
            outMap[vStr].Remove(e);
            edgeCountValue--;
        }

        return this;
    }

    public List<Edge>? InEdges(string v, string? w = null)
    {
        if (isDirected)
        {
            return inMap.TryGetValue(v, out var setV) ? FilterEdges(setV, v, w) : null;
        }

        return NodeEdges(v, w);
    }

    public List<Edge>? OutEdges(string v, string? w = null)
    {
        if (isDirected)
        {
            return outMap.TryGetValue(v, out var setV) ? FilterEdges(setV, v, w) : null;
        }

        return NodeEdges(v, w);
    }

    public List<Edge>? NodeEdges(string v, string? w = null)
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

            return FilterEdges(combined, v, w);
        }

        return null;
    }

    void RemoveFromParentsChildList(string v)
    {
        childrenMap![parentMap![v]].Remove(v);
    }

    static List<Edge> FilterEdges(OrderedMap<Edge> setV, string localEdge, string? remoteEdge)
    {
        var edges = setV.Values();
        if (remoteEdge == null)
        {
            return edges;
        }

        return edges.Where(edge =>
            (edge.V == localEdge && edge.W == remoteEdge) ||
            (edge.V == remoteEdge && edge.W == localEdge)).ToList();
    }

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
        if (map.TryGetValue(k, out var count))
        {
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
    }

    static string EdgeArgsToId(bool isDirected, string vIn, string wIn, string? name)
    {
        var v = vIn;
        var w = wIn;
        if (!isDirected && string.CompareOrdinal(v, w) > 0)
        {
            (v, w) = (w, v);
        }

        return v + EdgeKeyDelim + w + EdgeKeyDelim + (name ?? DefaultEdgeName);
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
