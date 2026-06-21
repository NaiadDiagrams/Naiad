namespace Naiad.Dagre;

/// <summary>Resolved entry: {vs, i, barycenter?, weight?}.</summary>
sealed class ResolvedEntry
{
    public List<string> Vs = [];
    public int I;
    public double? Barycenter;
    public double? Weight;
}