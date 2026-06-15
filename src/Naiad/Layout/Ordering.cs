internal static class Ordering
{
    const int maxIterations = 24;

    public static void Run(LayoutGraph graph) => new Runner(graph).Run();

    /// <summary>
    /// Reorders nodes within their ranks so that same-rank groups stay
    /// contiguous (no foreign node lands between them) and <c>SameBefore</c>/
    /// <c>SameAfter</c> edges are honored (target left/right of source),
    /// preserving the crossing-minimized order as far as the constraints allow.
    /// </summary>
    public static void EnforceSameRankOrder(LayoutGraph graph)
    {
        var sameEdges = graph.Edges.Where(_ => _.IsSameRank).ToList();
        if (sameEdges.Count == 0)
        {
            return;
        }

        // first must be ordered before second (within its group).
        var before = new List<(string first, string second)>();
        foreach (var edge in sameEdges)
        {
            switch (edge.RankConstraint)
            {
                case RankConstraint.SameBefore:
                    before.Add((edge.TargetId, edge.SourceId));
                    break;
                case RankConstraint.SameAfter:
                    before.Add((edge.SourceId, edge.TargetId));
                    break;
            }
        }

        foreach (var rank in graph.Ranks)
        {
            ReorderRank(rank, sameEdges, before);
        }

        graph.UpdateOrderInRanks();
    }

    static void ReorderRank(
        List<LayoutNode> rank,
        List<LayoutEdge> sameEdges,
        List<(string first, string second)> before)
    {
        var ids = new HashSet<string>(rank.Select(_ => _.Id));

        // Union nodes joined by a same-rank edge so each group stays contiguous.
        var parent = rank.ToDictionary(_ => _.Id, _ => _.Id);

        string Find(string x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }

            return x;
        }

        var grouped = false;
        foreach (var edge in sameEdges)
        {
            if (ids.Contains(edge.SourceId) &&
                ids.Contains(edge.TargetId))
            {
                parent[Find(edge.SourceId)] = Find(edge.TargetId);
                grouped = true;
            }
        }

        if (!grouped)
        {
            return;
        }

        // Members of each group, in the current (crossing-minimized) order.
        var groups = new Dictionary<string, List<LayoutNode>>();
        foreach (var node in rank.OrderBy(_ => _.Order))
        {
            var key = Find(node.Id);
            if (!groups.TryGetValue(key, out var list))
            {
                list = [];
                groups[key] = list;
            }

            list.Add(node);
        }

        // Order multi-node groups by their before/after constraints, then place
        // each group (and singleton) contiguously, anchored at its earliest
        // member so the overall arrangement is preserved.
        var blocks = groups.Values
            .Select(members => (anchor: members.Min(_ => _.Order), members: SortGroup(members, before)))
            .OrderBy(_ => _.anchor)
            .ToList();

        var order = 0;
        foreach (var (_, members) in blocks)
        {
            foreach (var node in members)
            {
                node.Order = order++;
            }
        }
    }

    static List<LayoutNode> SortGroup(List<LayoutNode> members, List<(string first, string second)> before)
    {
        if (members.Count < 2)
        {
            return members;
        }

        var ids = new HashSet<string>(members.Select(_ => _.Id));
        var indegree = members.ToDictionary(_ => _.Id, _ => 0);
        var successors = members.ToDictionary(_ => _.Id, _ => new List<string>());
        foreach (var (first, second) in before)
        {
            if (ids.Contains(first) &&
                ids.Contains(second))
            {
                successors[first].Add(second);
                indegree[second]++;
            }
        }

        // Stable topological sort: among nodes with no unmet predecessor, take
        // the one with the smallest current order.
        var ordered = new List<LayoutNode>(members.Count);
        var remaining = new List<LayoutNode>(members.OrderBy(_ => _.Order));
        while (ordered.Count < members.Count)
        {
            var next = remaining.FirstOrDefault(_ => indegree[_.Id] == 0);
            if (next is null)
            {
                // Constraint cycle - keep the remaining nodes in current order.
                ordered.AddRange(remaining);
                break;
            }

            ordered.Add(next);
            remaining.Remove(next);
            indegree[next.Id] = -1;
            foreach (var successorId in successors[next.Id])
            {
                indegree[successorId]--;
            }
        }

        return ordered;
    }

    sealed class Runner
    {
        static Comparison<(int sourceOrder, int targetOrder)> sortBySourceThenTarget = (a, b) =>
        {
            var cmp = a.sourceOrder.CompareTo(b.sourceOrder);
            return cmp != 0 ? cmp : a.targetOrder.CompareTo(b.targetOrder);
        };

        // Sort a rank by each node's median neighbour position (set in OrderByMedian), tie-broken by the
        // existing order for stability. Reads LayoutNode.MedianPosition directly — no per-comparison
        // dictionary lookup.
        static Comparison<LayoutNode> sortByMedian = (a, b) =>
        {
            var cmp = a.MedianPosition.CompareTo(b.MedianPosition);
            return cmp == 0 ? a.Order.CompareTo(b.Order) : cmp;
        };

        LayoutGraph graph;
        List<double> neighborOrders = [];
        List<(int sourceOrder, int targetOrder)> crossingsEdges = [];
        int[] targetOrders;
        int[] mergeBuffer;
        int[] bestOrders;

        public Runner(LayoutGraph graph)
        {
            this.graph = graph;
            targetOrders = new int[graph.Edges.Count];
            mergeBuffer = new int[graph.Edges.Count];
            bestOrders = new int[graph.Nodes.Count];
        }

        public void Run()
        {
            graph.BuildRanks();
            InitializeOrder();
            SaveOrders();

            var bestCrossings = CountCrossings();

            for (var i = 0; i < maxIterations && bestCrossings > 0; i++)
            {
                if (i % 2 == 0)
                {
                    SweepDown();
                }
                else
                {
                    SweepUp();
                }

                var crossings = CountCrossings();
                if (crossings < bestCrossings)
                {
                    bestCrossings = crossings;
                    SaveOrders();
                }
            }

            RestoreOrders();
            graph.UpdateOrderInRanks();
        }

        void InitializeOrder()
        {
            foreach (var rank in graph.Ranks)
            {
                for (var i = 0; i < rank.Count; i++)
                {
                    rank[i].Order = i;
                }
            }
        }

        void SweepDown()
        {
            for (var r = 1; r < graph.Ranks.Length; r++)
            {
                OrderByMedian(r, true);
            }
        }

        void SweepUp()
        {
            for (var r = graph.Ranks.Length - 2; r >= 0; r--)
            {
                OrderByMedian(r, false);
            }
        }

        void OrderByMedian(int rank, bool useInEdges)
        {
            var nodesInRank = graph.Ranks[rank];

            foreach (var node in nodesInRank)
            {
                neighborOrders.Clear();
                if (useInEdges)
                {
                    foreach (var edge in node.InEdges)
                    {
                        if (edge.Source is { } source)
                        {
                            neighborOrders.Add(source.Order);
                        }
                    }
                }
                else
                {
                    foreach (var edge in node.OutEdges)
                    {
                        if (edge.Target is { } target)
                        {
                            neighborOrders.Add(target.Order);
                        }
                    }
                }

                if (neighborOrders.Count == 0)
                {
                    node.MedianPosition = node.Order;
                }
                else
                {
                    neighborOrders.Sort();
                    node.MedianPosition = Median(neighborOrders);
                }
            }

            // Sort in-place by median position, maintaining stability for equal positions
            nodesInRank.Sort(sortByMedian);

            for (var i = 0; i < nodesInRank.Count; i++)
            {
                nodesInRank[i].Order = i;
            }
        }

        static double Median(List<double> values)
        {
            if (values.Count == 0)
            {
                return 0;
            }

            if (values.Count == 1)
            {
                return values[0];
            }

            if (values.Count == 2)
            {
                return (values[0] + values[1]) / 2;
            }

            var mid = values.Count / 2;
            if (values.Count % 2 == 0)
            {
                return (values[mid - 1] + values[mid]) / 2;
            }

            return values[mid];
        }

        int CountCrossings()
        {
            var total = 0;
            for (var r = 0; r < graph.Ranks.Length - 1; r++)
            {
                total += CountCrossingsBetweenRanks(r, r + 1);
            }

            return total;
        }

        int CountCrossingsBetweenRanks(int rank1, int rank2)
        {
            crossingsEdges.Clear();

            foreach (var node in graph.Ranks[rank1])
            {
                foreach (var edge in node.OutEdges)
                {
                    var target = edge.Target;
                    if (target is not null && target.Rank == rank2)
                    {
                        crossingsEdges.Add((node.Order, target.Order));
                    }
                }
            }

            if (crossingsEdges.Count <= 1)
            {
                return 0;
            }

            crossingsEdges.Sort(sortBySourceThenTarget);

            for (var i = 0; i < crossingsEdges.Count; i++)
            {
                targetOrders[i] = crossingsEdges[i].targetOrder;
            }

            return MergeSortCount(targetOrders, mergeBuffer, 0, crossingsEdges.Count - 1);
        }

        static int MergeSortCount(int[] arr, int[] buffer, int left, int right)
        {
            if (left >= right)
            {
                return 0;
            }

            var mid = left + (right - left) / 2;
            var count = MergeSortCount(arr, buffer, left, mid)
                      + MergeSortCount(arr, buffer, mid + 1, right);

            // Merge and count inversions
            var i = left;
            var j = mid + 1;
            var k = left;

            while (i <= mid && j <= right)
            {
                if (arr[i] <= arr[j])
                {
                    buffer[k++] = arr[i++];
                }
                else
                {
                    // All remaining elements in left half form inversions with arr[j]
                    count += mid - i + 1;
                    buffer[k++] = arr[j++];
                }
            }

            while (i <= mid)
            {
                buffer[k++] = arr[i++];
            }

            while (j <= right)
            {
                buffer[k++] = arr[j++];
            }

            Array.Copy(buffer, left, arr, left, right - left + 1);
            return count;
        }

        void SaveOrders()
        {
            var i = 0;
            foreach (var node in graph.Nodes.Values)
            {
                bestOrders[i++] = node.Order;
            }
        }

        void RestoreOrders()
        {
            var i = 0;
            foreach (var node in graph.Nodes.Values)
            {
                node.Order = bestOrders[i++];
            }
        }
    }
}
