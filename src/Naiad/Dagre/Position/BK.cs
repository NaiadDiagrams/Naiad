namespace Naiad.Dagre;

/*
 * This module provides coordinate assignment based on Brandes and Köpf, "Fast
 * and Simple Horizontal Coordinate Assignment."
 */
static class BK
{
    // type Conflicts = { [key: string]: { [key: string]: boolean } };
    // type PositionMap = { [key: string]: number };
    // type AlignmentResult = { root: { [key: string]: string }, align: { [key: string]: string } };
    // type XssMap = { [key: string]: PositionMap };

    internal sealed class AlignmentResult
    {
        public Dictionary<string, string> Root = new(StringComparer.Ordinal);
        public Dictionary<string, string> Align = new(StringComparer.Ordinal);
    }

    /*
     * Marks all edges in the graph with a type-1 conflict with the "type1Conflict"
     * property. A type-1 conflict is one where a non-inner segment crosses an
     * inner segment. An inner segment is an edge with both incident nodes marked
     * with the "dummy" property.
     *
     * This algorithm scans layer by layer, starting with the second, for type-1
     * conflicts between the current layer and the previous layer. For each layer
     * it scans the nodes from left to right until it reaches one that is incident
     * on an inner segment. It then scans predecessors to determine if they have
     * edges that cross that inner segment. At the end a final scan is done for all
     * nodes on the current rank to see if they cross the last visited inner
     * segment.
     *
     * This algorithm (safely) assumes that a dummy node will only be incident on a
     * single node in the layers being scanned.
     */
    internal static Dictionary<string, Dictionary<string, bool>> FindType1Conflicts(Graph graph, List<List<string>> layering)
    {
        var conflicts = new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);

        List<string> VisitLayer(List<string> prevLayer, List<string> layer)
        {
            var k0 =
                // last visited node in the previous layer that is incident on an inner
                // segment.
                0;
            // Tracks the last node in this layer scanned for crossings with a type-1
            // segment.
            var scanPos = 0;
            var prevLayerLength = prevLayer.Count;
            var lastNode = layer[^1];

            for (var i = 0; i < layer.Count; i++)
            {
                var v = layer[i];
                var w = FindOtherInnerSegmentNode(graph, v);
                var k1 = w != null ? graph.Node(w).Order!.Value : prevLayerLength;

                if (w != null || v == lastNode)
                {
                    foreach (var scanNode in layer.GetRange(scanPos, i + 1 - scanPos))
                    {
                        var preds = graph.Predecessors(scanNode);
                        if (preds != null)
                        {
                            foreach (var u in preds)
                            {
                                var uLabel = graph.Node(u);
                                var uPos = uLabel.Order!.Value;
                                if ((uPos < k0 || k1 < uPos) &&
                                    !(uLabel.Dummy != null && graph.Node(scanNode).Dummy != null))
                                {
                                    AddConflict(conflicts, u, scanNode);
                                }
                            }
                        }
                    }

                    scanPos = i + 1;
                    k0 = k1;
                }
            }

            return layer;
        }

        if (layering.Count != 0)
        {
            // layering.reduce(visitLayer)
            var acc = layering[0];
            for (var i = 1; i < layering.Count; i++)
            {
                acc = VisitLayer(acc, layering[i]);
            }
        }

        return conflicts;
    }

    internal static Dictionary<string, Dictionary<string, bool>> FindType2Conflicts(Graph graph, List<List<string>> layering)
    {
        var conflicts = new Dictionary<string, Dictionary<string, bool>>(StringComparer.Ordinal);

        void Scan(List<string> south, int southPos, int southEnd, int prevNorthBorder, int nextNorthBorder)
        {
            foreach (var i in Util.Range(southPos, southEnd))
            {
                var v = i >= 0 && i < south.Count ? south[i] : null;
                if (v == null)
                {
                    continue;
                }

                if (graph.Node(v).Dummy != null)
                {
                    var preds = graph.Predecessors(v);
                    if (preds != null)
                    {
                        foreach (var u in preds)
                        {
                            if (u == null)
                            {
                                continue;
                            }

                            var uNode = graph.Node(u);
                            if (uNode.Dummy != null &&
                                (uNode.Order!.Value < prevNorthBorder || uNode.Order!.Value > nextNorthBorder))
                            {
                                AddConflict(conflicts, u, v);
                            }
                        }
                    }
                }
            }
        }

        List<string> VisitLayer(List<string> north, List<string> south)
        {
            var prevNorthPos = -1;
            var nextNorthPos = -1;
            var southPos = 0;

            for (var southLookahead = 0; southLookahead < south.Count; southLookahead++)
            {
                var v = south[southLookahead];
                if (graph.Node(v).Dummy == "border")
                {
                    var predecessors = graph.Predecessors(v);
                    if (predecessors != null && predecessors.Count != 0)
                    {
                        var firstPred = predecessors[0];
                        if (firstPred == null)
                        {
                            continue;
                        }

                        nextNorthPos = graph.Node(firstPred).Order!.Value;
                        Scan(south, southPos, southLookahead, prevNorthPos, nextNorthPos);
                        southPos = southLookahead;
                        prevNorthPos = nextNorthPos;
                    }
                }

                Scan(south, southPos, south.Count, nextNorthPos, north.Count);
            }

            return south;
        }

        if (layering.Count != 0)
        {
            var acc = layering[0];
            for (var i = 1; i < layering.Count; i++)
            {
                acc = VisitLayer(acc, layering[i]);
            }
        }

        return conflicts;
    }

    internal static string? FindOtherInnerSegmentNode(Graph graph, string v)
    {
        if (graph.Node(v).Dummy != null)
        {
            var preds = graph.Predecessors(v);
            if (preds != null)
            {
                return preds.FirstOrDefault(u => graph.Node(u).Dummy != null);
            }
        }

        return null;
    }

    internal static void AddConflict(Dictionary<string, Dictionary<string, bool>> conflicts, string v, string w)
    {
        if (string.CompareOrdinal(v, w) > 0)
        {
            (v, w) = (w, v);
        }

        if (!conflicts.TryGetValue(v, out var conflictsV))
        {
            conflicts[v] = conflictsV = new(StringComparer.Ordinal);
        }

        conflictsV[w] = true;
    }

    internal static bool HasConflict(Dictionary<string, Dictionary<string, bool>> conflicts, string v, string w)
    {
        if (string.CompareOrdinal(v, w) > 0)
        {
            var tmp = v;
            v = w;
            w = tmp;
        }

        return conflicts.TryGetValue(v, out var conflictsV) && conflictsV.ContainsKey(w);
    }

    /*
     * Try to align nodes into vertical "blocks" where possible. This algorithm
     * attempts to align a node with one of its median neighbors. If the edge
     * connecting a neighbor is a type-1 conflict then we ignore that possibility.
     * If a previous node has already formed a block with a node after the node
     * we're trying to form a block with, we also ignore that possibility - our
     * blocks would be split in that scenario.
     */
    internal static AlignmentResult VerticalAlignment(
        Graph graph,
        List<List<string>> layering,
        Dictionary<string, Dictionary<string, bool>> conflicts,
        Func<string, OrderedMap<int>.KeyEnumerable> neighborFn)
    {
        var root = new Dictionary<string, string>(StringComparer.Ordinal);
        var align = new Dictionary<string, string>(StringComparer.Ordinal);
        var pos = new Dictionary<string, int>(StringComparer.Ordinal);

        // We cache the position here based on the layering because the graph and
        // layering may be out of sync. The layering matrix is manipulated to
        // generate different extreme alignments.
        foreach (var layer in layering)
        {
            for (var order = 0; order < layer.Count; order++)
            {
                var v = layer[order];
                root[v] = v;
                align[v] = v;
                pos[v] = order;
            }
        }

        // Reused across nodes: each node fully processes its neighbours before the next clears it.
        var ws = new List<string>();
        foreach (var layer in layering)
        {
            var prevIdx = -1;
            foreach (var v in layer)
            {
                ws.Clear();
                foreach (var w in neighborFn(v))
                {
                    ws.Add(w);
                }

                if (ws.Count != 0)
                {
                    // ws = wsRaw.sort((a, b) => pos[a] - pos[b]); JS sort is stable. Neighbour lists are
                    // tiny, so a stable insertion sort matches that ordering without the OrderBy/ToList
                    // allocation on this 4×-per-layout path.
                    for (var si = 1; si < ws.Count; ++si)
                    {
                        var key = ws[si];
                        var keyPos = pos.GetValueOrDefault(key, 0);
                        var sj = si - 1;
                        while (sj >= 0 && pos.GetValueOrDefault(ws[sj], 0) > keyPos)
                        {
                            ws[sj + 1] = ws[sj];
                            --sj;
                        }

                        ws[sj + 1] = key;
                    }

                    var mp = (ws.Count - 1) / 2.0;
                    for (int i = (int) Math.Floor(mp), il = (int) Math.Ceiling(mp); i <= il; ++i)
                    {
                        var w = i >= 0 && i < ws.Count ? ws[i] : null;
                        if (w == null)
                        {
                            continue;
                        }

                        if (pos.TryGetValue(w, out var posW) && align[v] == v &&
                            prevIdx < posW &&
                            !HasConflict(conflicts, v, w))
                        {
                            if (root.TryGetValue(w, out var rootW))
                            {
                                align[w] = v;
                                align[v] = root[v] = rootW;
                                prevIdx = posW;
                            }
                        }
                    }
                }
            }
        }

        return new() { Root = root, Align = align };
    }

    internal static Dictionary<string, double> HorizontalCompaction(
        Graph graph,
        List<List<string>> layering,
        Dictionary<string, string> root,
        Dictionary<string, string> align,
        bool reverseSep = false)
    {
        // This portion of the algorithm differs from BK due to a number of problems.
        // Instead of their algorithm we construct a new block graph and do two
        // sweeps. The first sweep places blocks with the smallest possible
        // coordinates. The second sweep removes unused space by moving blocks to the
        // greatest coordinates without violating separation.
        var xs = new Dictionary<string, double>(StringComparer.Ordinal);
        var blockG = BuildBlockGraph(graph, layering, root, reverseSep);
        var borderType = reverseSep ? "borderLeft" : "borderRight";

        void Iterate(Action<string> setXsFunc, Func<string, OrderedMap<int>.KeyEnumerable> nextNodesFunc)
        {
            var stack = blockG.Nodes(); // Create a copy of the node list.
            var visited = new Dictionary<string, bool>(StringComparer.Ordinal);
            var elem = Pop(stack);

            while (elem != null)
            {
                if (visited.TryGetValue(elem, out var seen) && seen)
                {
                    setXsFunc(elem);
                }
                else
                {
                    visited[elem] = true;
                    // Put the element back into the stack, so that we can process it
                    // again after all of the `nextNodesFunc` items are processed.
                    stack.Add(elem);
                    foreach (var nextElem in nextNodesFunc(elem))
                    {
                        stack.Add(nextElem);
                    }
                }

                elem = Pop(stack);
            }
        }

        // First pass, assign smallest coordinates. An absent block has no in-edges, so the loop leaves
        // acc at 0 — the same result the explicit null branch produced.
        void Pass1(string elem)
        {
            double acc = 0;
            foreach (var e in blockG.InEdgesOf(elem))
            {
                var xsV = xs.GetValueOrDefault(e.V, 0);
                var edgeWeight = blockG.Edge_(e);
                acc = Math.Max(acc, xsV + (edgeWeight?.Weight is { } ew ? ew : 0));
            }

            xs[elem] = acc;
        }

        // Second pass, assign greatest coordinates. An absent block has no out-edges, so min stays
        // +Infinity and the assignment below is skipped — matching the original null branch.
        void Pass2(string elem)
        {
            var min = double.PositiveInfinity;
            foreach (var e in blockG.OutEdgesOf(elem))
            {
                var xsW = xs.TryGetValue(e.W, out var xw) ? (double?) xw : null;
                var edgeWeight = blockG.Edge_(e);
                min = Math.Min(min, (xsW ?? 0) - (edgeWeight?.Weight is { } ew ? ew : 0));
            }

            var node = graph.Node(elem);
            if (!double.IsPositiveInfinity(min) && node.BorderType != borderType)
            {
                xs[elem] = Math.Max(xs.GetValueOrDefault(elem, 0), min);
            }
        }

        OrderedMap<int>.KeyEnumerable PredecessorsWrapper(string elem) => blockG.PredecessorsOf(elem);

        OrderedMap<int>.KeyEnumerable SuccessorsWrapper(string elem) => blockG.SuccessorsOf(elem);

        Iterate(Pass1, PredecessorsWrapper);
        Iterate(Pass2, SuccessorsWrapper);

        // Assign x coordinates to all nodes
        foreach (var v in align.Keys)
        {
            if (root.TryGetValue(v, out var rootV))
            {
                xs[v] = xs.GetValueOrDefault(rootV, 0);
            }
        }

        return xs;
    }

    static string? Pop(List<string> stack)
    {
        if (stack.Count == 0)
        {
            return null;
        }

        var last = stack[^1];
        stack.RemoveAt(stack.Count - 1);
        return last;
    }

    internal static Graph BuildBlockGraph(
        Graph graph,
        List<List<string>> layering,
        Dictionary<string, string> root,
        bool reverseSep)
    {
        var blockGraph = new Graph();
        var graphLabel = graph.Graph_();
        var sepFn = Sep(graphLabel.Nodesep!.Value, graphLabel.Edgesep!.Value, reverseSep);

        foreach (var layer in layering)
        {
            string? u = null;
            foreach (var v in layer)
            {
                if (root.TryGetValue(v, out var vRoot))
                {
                    blockGraph.SetNode(vRoot);
                    if (u != null)
                    {
                        if (root.TryGetValue(u, out var uRoot))
                        {
                            var prevMax = blockGraph.Edge_(uRoot, vRoot);
                            var prevMaxVal = prevMax?.Weight ?? 0;
                            blockGraph.SetEdge(uRoot, vRoot, new() { Weight = Math.Max(sepFn(graph, v, u), prevMaxVal) });
                        }
                    }

                    u = v;
                }
            }
        }

        return blockGraph;
    }

    /*
     * Returns the alignment that has the smallest width of the given alignments.
     */
    internal static Dictionary<string, double> FindSmallestWidthAlignment(Graph graph, Dictionary<string, Dictionary<string, double>> xss)
    {
        var currentMin = double.PositiveInfinity;
        Dictionary<string, double>? currentXs = null;

        foreach (var xs in xss.Values)
        {
            var max = double.NegativeInfinity;
            var min = double.PositiveInfinity;

            foreach (var (v, x) in xs)
            {
                var halfWidth = Width(graph, v) / 2;

                max = Math.Max(x + halfWidth, max);
                min = Math.Min(x - halfWidth, min);
            }

            var newMin = max - min;
            if (newMin < currentMin)
            {
                currentMin = newMin;
                currentXs = xs;
            }
        }

        return currentXs!;
    }

    /*
     * Align the coordinates of each of the layout alignments such that
     * left-biased alignments have their minimum coordinate at the same point as
     * the minimum coordinate of the smallest width alignment and right-biased
     * alignments have their maximum coordinate at the same point as the maximum
     * coordinate of the smallest width alignment.
     */
    internal static void AlignCoordinates(Dictionary<string, Dictionary<string, double>> xss, Dictionary<string, double> alignTo)
    {
        var alignToVals = alignTo.Values.ToList();
        var alignToMin = Util.ApplyMin(alignToVals);
        var alignToMax = Util.ApplyMax(alignToVals);

        foreach (var vert in new[] { "u", "d" })
        {
            foreach (var horiz in new[] { "l", "r" })
            {
                var alignment = vert + horiz;
                var xs = xss.GetValueOrDefault(alignment);

                if (xs == null || ReferenceEquals(xs, alignTo))
                {
                    continue;
                }

                var xsVals = xs.Values.ToList();
                var delta = alignToMin - Util.ApplyMin(xsVals);
                if (horiz != "l")
                {
                    delta = alignToMax - Util.ApplyMax(xsVals);
                }

                if (delta != 0)
                {
                    xss[alignment] = Util.MapValues(xs, (x, _) => x + delta);
                }
            }
        }
    }

    internal static Dictionary<string, double> Balance(Dictionary<string, Dictionary<string, double>> xss, string? align = null)
    {
        var ulMap = xss.GetValueOrDefault("ul");
        if (ulMap == null)
        {
            return new(StringComparer.Ordinal);
        }

        return Util.MapValues(ulMap, (num, v) =>
        {
            if (align != null)
            {
                var alignmentKey = align.ToLowerInvariant();
                var alignment = xss.GetValueOrDefault(alignmentKey);
                if (alignment != null && alignment.TryGetValue(v, out var av))
                {
                    return av;
                }
            }

            var xs = xss.Values
                .Select(x => x.GetValueOrDefault(v, 0))
                .OrderBy(a => a)
                .ToList();
            return ((xs.Count > 1 ? xs[1] : 0) + (xs.Count > 2 ? xs[2] : 0)) / 2;
        });
    }

    internal static Dictionary<string, double> PositionX(Graph graph)
    {
        var layering = Util.BuildLayerMatrix(graph);

        // Object.assign(findType1Conflicts(...), findType2Conflicts(...))
        var conflicts = FindType1Conflicts(graph, layering);
        var type2 = FindType2Conflicts(graph, layering);
        foreach (var (k, v) in type2)
        {
            conflicts[k] = v;
        }

        var xss = new Dictionary<string, Dictionary<string, double>>(StringComparer.Ordinal);
        List<List<string>> adjustedLayering = layering;
        foreach (var vert in new[] { "u", "d" })
        {
            // vert === "u" ? layering : Object.values(layering).reverse()
            adjustedLayering = vert == "u" ? layering : Enumerable.Reverse(layering).ToList();
            foreach (var horiz in new[] { "l", "r" })
            {
                if (horiz == "r")
                {
                    adjustedLayering = adjustedLayering.Select(inner => Enumerable.Reverse(inner).ToList()).ToList();
                }

                OrderedMap<int>.KeyEnumerable NeighborFn(string v) =>
                    vert == "u" ? graph.PredecessorsOf(v) : graph.SuccessorsOf(v);

                var align = VerticalAlignment(graph, adjustedLayering, conflicts, NeighborFn);
                var xs = HorizontalCompaction(graph, adjustedLayering, align.Root, align.Align, horiz == "r");
                if (horiz == "r")
                {
                    xs = Util.MapValues(xs, (x, _) => -x);
                }

                xss[vert + horiz] = xs;
            }
        }

        var smallestWidth = FindSmallestWidthAlignment(graph, xss);
        AlignCoordinates(xss, smallestWidth);
        return Balance(xss, graph.Graph_().Align);
    }

    internal static Func<Graph, string, string, double> Sep(double nodeSep, double edgeSep, bool reverseSep)
    {
        return (g, v, w) =>
        {
            var vLabel = g.Node(v);
            var wLabel = g.Node(w);
            double sum = 0;
            double? delta = null;

            sum += vLabel.Width / 2;
            if (vLabel.Labelpos != null)
            {
                switch (vLabel.Labelpos.ToLowerInvariant())
                {
                    case "l":
                        delta = -vLabel.Width / 2;
                        break;
                    case "r":
                        delta = vLabel.Width / 2;
                        break;
                }
            }

            if (delta is { } d1 && d1 != 0)
            {
                sum += reverseSep ? d1 : -d1;
            }

            delta = null;

            sum += (vLabel.Dummy != null ? edgeSep : nodeSep) / 2;
            sum += (wLabel.Dummy != null ? edgeSep : nodeSep) / 2;

            sum += wLabel.Width / 2;
            if (wLabel.Labelpos != null)
            {
                switch (wLabel.Labelpos.ToLowerInvariant())
                {
                    case "l":
                        delta = wLabel.Width / 2;
                        break;
                    case "r":
                        delta = -wLabel.Width / 2;
                        break;
                }
            }

            if (delta is { } d2 && d2 != 0)
            {
                sum += reverseSep ? d2 : -d2;
            }

            return sum;
        };
    }

    internal static double Width(Graph graph, string v) => graph.Node(v).Width;
}
