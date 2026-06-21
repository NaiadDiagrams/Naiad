namespace Naiad.Dagre;

static class Barycenter
{
    public static List<BarycenterEntry> Run(Graph graph, List<string> movable)
    {
        return movable.Select(v =>
        {
            var inV = graph.InEdges(v);
            if (inV == null || inV.Count == 0)
            {
                return new() { V = v };
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
