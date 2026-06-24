namespace Benchmarks;

// Measures the cost of reaching every edge's label two ways:
//   - the old shape: walk Edges(), then FindEdgeLabel(e) re-derives each edge's id (string concat) and re-hashes it.
//   - the new shape: walk EdgeLabels() straight off the label map.
[MemoryDiagnoser]
public class EdgeLabelBenchmarks
{
    [Params(100, 1000)]
    public int EdgeCount;

    Graph graph = null!;

    [GlobalSetup]
    public void Setup()
    {
        graph = new(multigraph: true);
        for (var i = 0; i < EdgeCount; i++)
        {
            graph.SetEdge("n" + i, "n" + (i + 1), new() { Weight = i });
        }
    }

    [Benchmark(Baseline = true)]
    public double EdgesThenFindLabel()
    {
        var acc = 0d;
        foreach (var e in graph.Edges())
        {
            acc += graph.FindEdgeLabel(e).Weight ?? 0;
        }

        return acc;
    }

    [Benchmark]
    public double EdgeLabelsDirect()
    {
        var acc = 0d;
        foreach (var label in graph.EdgeLabels())
        {
            acc += label.Weight ?? 0;
        }

        return acc;
    }
}
