namespace Naiad.Diagrams.GitGraph;

public class ComputedGitGraph
{
    public List<GitBranch> Branches { get; } = [];
    public List<GitCommit> Commits { get; } = [];
    public Dictionary<string, GitCommit> CommitMap { get; } = [];

    Dictionary<string, GitBranch>? branchByName;

    /// <summary>Branch lookup by name, O(1) instead of a per-commit/per-connection <c>List.Find</c> scan.
    /// Lazily (re)built from <see cref="Branches"/>.</summary>
    public GitBranch? FindBranch(string name)
    {
        if (branchByName == null || branchByName.Count != Branches.Count)
        {
            branchByName = new(Branches.Count, StringComparer.Ordinal);
            foreach (var branch in Branches)
            {
                branchByName[branch.Name] = branch;
            }
        }

        return branchByName.GetValueOrDefault(name);
    }
}