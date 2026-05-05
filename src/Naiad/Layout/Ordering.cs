namespace Naiad.Layout;

internal static class Ordering
{
    const int MaxIterations = 24;

    public static void Run(LayoutGraph graph) => new Runner(graph).Run();

    sealed class Runner
    {
        static readonly Comparison<(int sourceOrder, int targetOrder)> sortBySourceThenTarget = (a, b) =>
        {
            var cmp = a.sourceOrder.CompareTo(b.sourceOrder);
            return cmp != 0 ? cmp : a.targetOrder.CompareTo(b.targetOrder);
        };

        readonly LayoutGraph graph;
        readonly Dictionary<string, double> positions = [];
        readonly List<double> neighborOrders = [];
        readonly List<(int sourceOrder, int targetOrder)> crossingsEdges = [];
        readonly int[] targetOrders;
        readonly int[] mergeBuffer;
        readonly int[] bestOrders;
        readonly Comparison<LayoutNode> sortByMedian;

        public Runner(LayoutGraph graph)
        {
            this.graph = graph;
            targetOrders = new int[graph.Edges.Count];
            mergeBuffer = new int[graph.Edges.Count];
            bestOrders = new int[graph.Nodes.Count];
            sortByMedian = (a, b) =>
            {
                var cmp = positions[a.Id].CompareTo(positions[b.Id]);
                return cmp == 0 ? a.Order.CompareTo(b.Order) : cmp;
            };
        }

        public void Run()
        {
            graph.BuildRanks();
            InitializeOrder();
            SaveOrders();

            var bestCrossings = CountCrossings();

            for (var i = 0; i < MaxIterations && bestCrossings > 0; i++)
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
            positions.Clear();

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
                    positions[node.Id] = node.Order;
                }
                else
                {
                    neighborOrders.Sort();
                    positions[node.Id] = Median(neighborOrders);
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
