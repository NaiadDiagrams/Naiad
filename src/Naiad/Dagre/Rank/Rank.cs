static class Rank
{
    /*
     * Assigns a rank to each node in the input graph that respects the "minlen"
     * constraint specified on edges between nodes.
     *
     * This basic structure is derived from Gansner, et al., "A Technique for
     * Drawing Directed Graphs."
     *
     * Pre-conditions:
     *
     *    1. Graph must be a connected DAG
     *    2. Graph nodes must be objects
     *    3. Graph edges must have "weight" and "minlen" attributes
     *
     * Post-conditions:
     *
     *    1. Graph nodes will have a "rank" attribute based on the results of the
     *       algorithm. Ranks can start at any index (including negative), we'll
     *       fix them up later.
     */
    public static void Run(Graph graph) => NetworkSimplex.Run(graph);
}
