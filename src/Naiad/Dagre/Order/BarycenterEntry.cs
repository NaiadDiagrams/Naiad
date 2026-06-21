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