namespace Naiad.Dagre;

/// <summary>
/// Barycenter entry: {v, barycenter?, weight?}. Carries an optional <see cref="Vs"/> list because the same
/// shape is reused (and mutated) by sort-subgraph's <c>mergeBarycenters</c> path. Faithful to the TS
/// <c>BarycenterEntry</c> interface.
/// </summary>
sealed class BarycenterEntry
{
    public string V = "";
    public double? Barycenter;
    public double? Weight;
    public List<string>? Vs;
}

/// <summary>Faithful port of dagre's <c>order/barycenter.ts</c>.</summary>
static class Barycenter
{
    public static List<BarycenterEntry> Run(Graph graph, List<string> movable)
    {
        return movable.Select(v =>
        {
            var inV = graph.InEdges(v);
            if (inV == null || inV.Count == 0)
            {
                return new BarycenterEntry { V = v };
            }

            var sum = 0d;
            var weight = 0d;
            foreach (var e in inV)
            {
                var edge = graph.Edge_(e);
                var nodeU = graph.Node(e.V);
                sum += edge.Weight!.Value * nodeU.Order!.Value;
                weight += edge.Weight!.Value;
            }

            return new BarycenterEntry
            {
                V = v,
                Barycenter = sum / weight,
                Weight = weight
            };
        }).ToList();
    }
}
