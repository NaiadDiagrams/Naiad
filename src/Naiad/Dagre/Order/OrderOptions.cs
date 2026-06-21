namespace Naiad.Dagre;

/// <summary>Options for <see cref="Order.Run"/>. Faithful to the TS <c>OrderOptions</c> interface.
/// (<c>OrderConstraint</c> is defined alongside <see cref="LayoutOptions"/> in Layout.cs.)</summary>
sealed class OrderOptions
{
    public Action<Graph, Action<Graph, OrderOptions>>? CustomOrder;
    public bool? DisableOptimalOrderHeuristic;
    public List<OrderConstraint>? Constraints;
}