static class Acyclic
{
    public static void Run(LayoutGraph graph) =>
        new Runner(graph).Run();

    public static void Undo(LayoutGraph graph)
    {
        foreach (var edge in graph.Edges.Where(_ => _.IsReversed))
        {
            edge.IsReversed = false;
            // Swap back
            var source = graph.GetNode(edge.SourceId);
            if (source is null)
            {
                continue;
            }

            var target = graph.GetNode(edge.TargetId);
            if (target is null)
            {
                continue;
            }

            target.OutEdges.Remove(edge);
            source.InEdges.Remove(edge);
            source.OutEdges.Add(edge);
            target.InEdges.Add(edge);
        }
    }

    sealed class Runner(LayoutGraph graph)
    {
        readonly HashSet<string> visited = [];
        readonly HashSet<string> stack = [];
        readonly List<LayoutEdge> edgesToReverse = [];

        public void Run()
        {
            foreach (var node in graph.Nodes.Values)
            {
                if (!visited.Contains(node.Id))
                {
                    Dfs(node.Id);
                }
            }

            // Reverse back edges to break cycles
            foreach (var edge in edgesToReverse)
            {
                edge.IsReversed = true;
                // Swap source and target in the graph
                var source = graph.GetNode(edge.SourceId);
                var target = graph.GetNode(edge.TargetId);
                if (source is null ||
                    target is null)
                {
                    continue;
                }

                source.OutEdges.Remove(edge);
                target.InEdges.Remove(edge);
                target.OutEdges.Add(edge);
                source.InEdges.Add(edge);
            }
        }

        void Dfs(string nodeId)
        {
            visited.Add(nodeId);
            stack.Add(nodeId);

            var node = graph.GetNode(nodeId);
            if (node is null)
            {
                return;
            }

            foreach (var edge in node.OutEdges)
            {
                if (stack.Contains(edge.TargetId))
                {
                    // Back edge found - this creates a cycle
                    edgesToReverse.Add(edge);
                }
                else if (!visited.Contains(edge.TargetId))
                {
                    Dfs(edge.TargetId);
                }
            }

            stack.Remove(nodeId);
        }
    }
}
