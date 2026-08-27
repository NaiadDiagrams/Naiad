namespace Naiad.Diagrams.GitGraph;

public class GitCommit
{
    public required string Id { get; init; }
    public string? Message { get; set; }
    public string? Tag { get; set; }
    public CommitType Type { get; set; } = CommitType.Normal;
    public required string Branch { get; init; }
    public List<string> Parents { get; } = [];

    /// <summary>
    /// What the commit is captioned with, when that differs from its id — a cherry-pick names the commit
    /// it copied. Falls back to <see cref="Id"/>.
    /// </summary>
    public string? Label { get; set; }

    /// <summary>How the commit came about, which decides its glyph independently of <see cref="Type"/>.</summary>
    public bool IsMerge { get; set; }

    public bool IsCherryPick { get; set; }

    public string DisplayLabel => Label ?? Id;

    // Layout properties
    public int Row { get; set; }
}
