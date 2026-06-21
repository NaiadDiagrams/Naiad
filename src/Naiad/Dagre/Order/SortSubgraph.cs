namespace Naiad.Dagre;

static class SortSubgraph
{
    public static SortResult Run(Graph graph, string v, Graph constraintGraph, bool biasRight = false)
    {
        var movable = graph.Children(v);
        var node = graph.Node(v);
        var bl = node?.BorderLeftId;
        var br = node?.BorderRightId;
        var subgraphs = new Dictionary<string, SortResult>(StringComparer.Ordinal);

        if (bl != null)
        {
            movable = movable.Where(w => w != bl && w != br).ToList();
        }

        var barycenters = Barycenter.Run(graph, movable);
        foreach (var entry in barycenters)
        {
            if (graph.Children(entry.V).Count != 0)
            {
                var subgraphResult = Run(graph, entry.V, constraintGraph, biasRight);
                subgraphs[entry.V] = subgraphResult;
                if (subgraphResult.Barycenter != null)
                {
                    MergeBarycenters(entry, subgraphResult);
                }
            }
        }

        var entries = ResolveConflicts.Run(barycenters, constraintGraph);
        ExpandSubgraphs(entries, subgraphs);

        var result = Sort.Run(entries, biasRight);

        if (bl != null && br != null)
        {
            result.Vs = new List<string> { bl }
                .Concat(result.Vs)
                .Concat(new[] { br })
                .ToList();
            var blPredecessors = graph.Predecessors(bl);
            if (blPredecessors != null && blPredecessors.Count != 0)
            {
                var blPred = graph.Node(blPredecessors[0]);
                var brPredecessors = graph.Predecessors(br);
                var brPred = graph.Node(brPredecessors![0]);
                if (result.Barycenter == null)
                {
                    result.Barycenter = 0;
                    result.Weight = 0;
                }

                result.Barycenter = (result.Barycenter!.Value * result.Weight!.Value +
                    blPred.Order!.Value + brPred.Order!.Value) / (result.Weight!.Value + 2);
                result.Weight = result.Weight!.Value + 2;
            }
        }

        return result;
    }

    static void ExpandSubgraphs(List<ResolvedEntry> entries, Dictionary<string, SortResult> subgraphs)
    {
        foreach (var entry in entries)
        {
            entry.Vs = entry.Vs.SelectMany<string, string>(v =>
            {
                if (subgraphs.TryGetValue(v, out var subgraph))
                {
                    return subgraph.Vs;
                }

                return [v];
            }).ToList();
        }
    }

    static void MergeBarycenters(BarycenterEntry target, SortResult other)
    {
        if (target.Barycenter != null)
        {
            target.Barycenter = (target.Barycenter.Value * target.Weight!.Value +
                    other.Barycenter!.Value * other.Weight!.Value) /
                (target.Weight!.Value + other.Weight!.Value);
            target.Weight = target.Weight!.Value + other.Weight!.Value;
        }
        else
        {
            target.Barycenter = other.Barycenter;
            target.Weight = other.Weight;
        }
    }
}
