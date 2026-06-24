namespace Naiad;

/// <summary>
/// Optional layout constraint on an edge, used by the layout engine to pin two
/// nodes to the same rank (and optionally order them).
/// </summary>
public enum RankConstraint
{
    /// <summary>Normal edge: the target is ranked one level below the source.</summary>
    None,

    /// <summary>The endpoints are placed on the same rank.</summary>
    Same,

    /// <summary>Same rank; the target is ordered before (left of) the source.</summary>
    SameBefore,

    /// <summary>Same rank; the target is ordered after (right of) the source.</summary>
    SameAfter
}
