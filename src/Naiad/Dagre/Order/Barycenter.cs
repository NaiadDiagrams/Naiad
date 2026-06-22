namespace Naiad.Dagre;

static class Barycenter
{
    public static List<BarycenterEntry> Run(Graph graph, List<string> movable)
    {
        var result = new List<BarycenterEntry>(movable.Count);
        foreach (var v in movable)
        {
            var sum = 0d;
            var weight = 0d;
            var hasInEdges = false;
            foreach (var e in graph.InEdgesOf(v))
            {
                hasInEdges = true;
                var edge = graph.FindEdgeLabel(e);
                var nodeU = graph.NodeLabel(e.V);
                sum += edge.Weight!.Value * nodeU.Order!.Value;
                weight += edge.Weight!.Value;
            }

            result.Add(hasInEdges
                ? new() { V = v, Barycenter = sum / weight, Weight = weight }
                : new() { V = v });
        }

        return result;
    }
}
