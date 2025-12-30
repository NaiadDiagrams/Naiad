namespace MermaidSharp.Diagrams.GitGraph;

public class GitGraphModel : DiagramBase
{
    public List<GitOperation> Operations { get; } = [];
    public string MainBranchName { get; set; } = "main";
    public int MainBranchOrder { get; set; }
}

public abstract class GitOperation
{
    public int Order { get; set; }
}

public class CommitOperation : GitOperation
{
    public string? Id { get; set; }
    public string? Message { get; set; }
    public string? Tag { get; set; }
    public CommitType Type { get; set; } = CommitType.Normal;
}

public class BranchOperation : GitOperation
{
    public required string Name { get; init; }
    public int? BranchOrder { get; set; }
}

public class CheckoutOperation : GitOperation
{
    public required string BranchName { get; init; }
}

public class MergeOperation : GitOperation
{
    public required string BranchName { get; init; }
    public string? Id { get; set; }
    public string? Tag { get; set; }
    public CommitType Type { get; set; } = CommitType.Normal;
}

public class CherryPickOperation : GitOperation
{
    public required string CommitId { get; init; }
    public string? Tag { get; set; }
}

public enum CommitType
{
    Normal,
    Reverse,
    Highlight
}

// Computed model for rendering
public class GitCommit
{
    public required string Id { get; init; }
    public string? Message { get; set; }
    public string? Tag { get; set; }
    public CommitType Type { get; set; } = CommitType.Normal;
    public required string Branch { get; init; }
    public List<string> Parents { get; } = [];

    // Layout properties
    public Position Position { get; set; }
    public int Column { get; set; }
    public int Row { get; set; }
}

public class GitBranch
{
    public required string Name { get; init; }
    public int Order { get; set; }
    public int Column { get; set; }
    public string? Color { get; set; }
    public List<GitCommit> Commits { get; } = [];
}

public class ComputedGitGraph
{
    public List<GitBranch> Branches { get; } = [];
    public List<GitCommit> Commits { get; } = [];
    public Dictionary<string, GitCommit> CommitMap { get; } = [];
}
