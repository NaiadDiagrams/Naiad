namespace MermaidSharp.Diagrams.GitGraph;

public class GitGraphRenderer : IDiagramRenderer<GitGraphModel>
{
    const double commitRadius = 12;
    const double commitSpacingX = 60;
    const double commitSpacingY = 50;
    const double branchLabelWidth = 80;
    const double tagHeight = 20;
    const double tagPadding = 5;

    static string[] branchColors =
    [
        "#4CAF50", // green - main
        "#2196F3", // blue
        "#FF9800", // orange
        "#9C27B0", // purple
        "#F44336", // red
        "#00BCD4", // cyan
        "#795548", // brown
        "#607D8B"  // blue-grey
    ];

    public SvgDocument Render(GitGraphModel model, RenderOptions options)
    {
        // Compute the actual git graph from operations
        var computed = ComputeGraph(model);

        // Calculate dimensions
        var maxRow = computed.Commits.Count > 0 ? computed.Commits.Max(_ => _.Row) : 0;
        var maxColumn = computed.Branches.Count > 0 ? computed.Branches.Max(_ => _.Column) : 0;

        var graphWidth = (maxRow + 1) * commitSpacingX + branchLabelWidth;
        var graphHeight = (maxColumn + 1) * commitSpacingY;

        var width = graphWidth + options.Padding * 2;
        var height = graphHeight + options.Padding * 2;

        var builder = new SvgBuilder().Size(width, height);

        var offsetX = options.Padding + branchLabelWidth;
        var offsetY = options.Padding + commitSpacingY / 2;

        // Draw branch labels
        foreach (var branch in computed.Branches)
        {
            var y = offsetY + branch.Column * commitSpacingY;
            var color = branch.Color ?? branchColors[branch.Column % branchColors.Length];

            builder.AddText(
                options.Padding + 5, y, branch.Name,
                anchor: "start",
                baseline: "middle",
                fontSize: $"{options.FontSize - 2}px",
                fontFamily: options.FontFamily,
                fill: color,
                fontWeight: "bold");
        }

        // Draw branch lines
        foreach (var branch in computed.Branches)
        {
            if (branch.Commits.Count == 0)
            {
                continue;
            }

            var y = offsetY + branch.Column * commitSpacingY;
            var color = branch.Color ?? branchColors[branch.Column % branchColors.Length];

            var firstCommit = branch.Commits.OrderBy(_ => _.Row).First();
            var lastCommit = branch.Commits.OrderBy(_ => _.Row).Last();

            var startX = offsetX + firstCommit.Row * commitSpacingX;
            var endX = offsetX + lastCommit.Row * commitSpacingX;

            builder.AddLine(
                startX,
                y,
                endX,
                y,
                stroke: color,
                strokeWidth: 2);
        }

        // Draw connections between commits (parent-child relationships)
        foreach (var commit in computed.Commits)
        {
            foreach (var parentId in commit.Parents)
            {
                if (computed.CommitMap.TryGetValue(parentId, out var parent))
                {
                    DrawConnection(builder, parent, commit, computed, offsetX, offsetY);
                }
            }
        }

        // Draw commits
        foreach (var commit in computed.Commits)
        {
            DrawCommit(builder, commit, computed, offsetX, offsetY, options);
        }

        return builder.Build();
    }

    static void DrawConnection(
        SvgBuilder builder,
        GitCommit from,
        GitCommit to,
        ComputedGitGraph graph,
        double offsetX,
        double offsetY)
    {
        var fromBranch = graph.Branches.Find(_ => _.Name == from.Branch);
        var toBranch = graph.Branches.Find(_ => _.Name == to.Branch);

        if (fromBranch == null || toBranch == null)
        {
            return;
        }

        var fromX = offsetX + from.Row * commitSpacingX;
        var fromY = offsetY + fromBranch.Column * commitSpacingY;
        var toX = offsetX + to.Row * commitSpacingX;
        var toY = offsetY + toBranch.Column * commitSpacingY;

        var toColor = toBranch.Color ?? branchColors[toBranch.Column % branchColors.Length];

        if (from.Branch == to.Branch)
        {
            // Same branch - straight line (already drawn as branch line)
            return;
        }

        // Different branches - draw curved connection (merge or branch point)
        // Use a simple path with control points
        var midX = (fromX + toX) / 2;

        var path = $"""
                    M {Fmt(fromX)} {Fmt(fromY)}
                    C {Fmt(midX)} {Fmt(fromY)}, {Fmt(midX)} {Fmt(toY)}, {Fmt(toX)} {Fmt(toY)}
                    """;

        builder.AddPath(path, stroke: toColor, strokeWidth: 2, fill: "none");
    }

    static void DrawCommit(SvgBuilder builder, GitCommit commit, ComputedGitGraph graph,
        double offsetX, double offsetY, RenderOptions options)
    {
        var branch = graph.Branches.Find(_ => _.Name == commit.Branch);
        if (branch == null)
        {
            return;
        }

        var x = offsetX + commit.Row * commitSpacingX;
        var y = offsetY + branch.Column * commitSpacingY;
        var color = branch.Color ?? branchColors[branch.Column % branchColors.Length];

        // Commit circle
        var fill = commit.Type switch
        {
            CommitType.Reverse => "#fff",
            CommitType.Highlight => "#FFD700",
            _ => color
        };

        var strokeWidth = commit.Type == CommitType.Reverse ? 3 : 2;

        builder.AddCircle(x, y, commitRadius,
            fill: fill,
            stroke: color,
            strokeWidth: strokeWidth);

        // Commit ID (abbreviated)
        var displayId = commit.Id.Length > 7 ? commit.Id[..7] : commit.Id;
        builder.AddText(x, y, displayId,
            anchor: "middle",
            baseline: "middle",
            fontSize: $"{options.FontSize - 4}px",
            fontFamily: options.FontFamily,
            fill: commit.Type == CommitType.Highlight ? "#000" : "#fff");

        // Tag
        if (!string.IsNullOrEmpty(commit.Tag))
        {
            var tagWidth = MeasureText(commit.Tag, options.FontSize - 2) + tagPadding * 2;
            var tagX = x - tagWidth / 2;
            var tagY = y - commitRadius - tagHeight - 5;

            builder.AddRect(tagX, tagY, tagWidth, tagHeight,
                rx: 3,
                fill: "#FFF9C4",
                stroke: "#FBC02D",
                strokeWidth: 1);

            builder.AddText(x, tagY + tagHeight / 2, commit.Tag,
                anchor: "middle",
                baseline: "middle",
                fontSize: $"{options.FontSize - 2}px",
                fontFamily: options.FontFamily,
                fill: "#333");
        }
    }

    static ComputedGitGraph ComputeGraph(GitGraphModel model)
    {
        var computed = new ComputedGitGraph();
        var branchMap = new Dictionary<string, GitBranch>();
        var currentBranch = model.MainBranchName;
        var commitCounter = 0;

        // Create main branch
        var mainBranch = new GitBranch
        {
            Name = model.MainBranchName,
            Order = model.MainBranchOrder,
            Column = 0
        };
        branchMap[model.MainBranchName] = mainBranch;
        computed.Branches.Add(mainBranch);

        string? lastCommitId = null;
        // branch -> latest commit id
        var branchHeads = new Dictionary<string, string>();

        foreach (var op in model.Operations)
        {
            switch (op)
            {
                case CommitOperation commit:
                    var commitId = commit.Id ?? $"commit{commitCounter}";
                    var gitCommit = new GitCommit
                    {
                        Id = commitId,
                        Message = commit.Message,
                        Tag = commit.Tag,
                        Type = commit.Type,
                        Branch = currentBranch,
                        Row = commitCounter
                    };

                    // Add parent (previous commit on this branch, or branch point)
                    if (branchHeads.TryGetValue(currentBranch, out var branchHead))
                    {
                        gitCommit.Parents.Add(branchHead);
                    }
                    else if (lastCommitId != null)
                    {
                        gitCommit.Parents.Add(lastCommitId);
                    }

                    computed.Commits.Add(gitCommit);
                    computed.CommitMap[commitId] = gitCommit;
                    branchHeads[currentBranch] = commitId;

                    if (branchMap.TryGetValue(currentBranch, out var commitBranch))
                    {
                        commitBranch.Commits.Add(gitCommit);
                    }

                    lastCommitId = commitId;
                    commitCounter++;
                    break;

                case BranchOperation branch:
                    if (!branchMap.ContainsKey(branch.Name))
                    {
                        var newBranch = new GitBranch
                        {
                            Name = branch.Name,
                            Order = branch.BranchOrder ?? computed.Branches.Count,
                            Column = computed.Branches.Count
                        };
                        branchMap[branch.Name] = newBranch;
                        computed.Branches.Add(newBranch);

                        // New branch starts from current branch's head
                        if (branchHeads.TryGetValue(currentBranch, out var parentCommit))
                        {
                            branchHeads[branch.Name] = parentCommit;
                        }
                    }
                    currentBranch = branch.Name;
                    break;

                case CheckoutOperation checkout:
                    currentBranch = checkout.BranchName;
                    break;

                case MergeOperation merge:
                    var mergeId = merge.Id ?? $"merge{commitCounter}";
                    var mergeCommit = new GitCommit
                    {
                        Id = mergeId,
                        Tag = merge.Tag,
                        Type = merge.Type,
                        Branch = currentBranch,
                        Row = commitCounter
                    };

                    // Merge has two parents: current branch head and merged branch head
                    if (branchHeads.TryGetValue(currentBranch, out var currentHead))
                    {
                        mergeCommit.Parents.Add(currentHead);
                    }

                    if (branchHeads.TryGetValue(merge.BranchName, out var mergedHead))
                    {
                        mergeCommit.Parents.Add(mergedHead);
                    }

                    computed.Commits.Add(mergeCommit);
                    computed.CommitMap[mergeId] = mergeCommit;
                    branchHeads[currentBranch] = mergeId;

                    if (branchMap.TryGetValue(currentBranch, out var mergeBranch))
                    {
                        mergeBranch.Commits.Add(mergeCommit);
                    }

                    lastCommitId = mergeId;
                    commitCounter++;
                    break;

                case CherryPickOperation cherryPick:
                    if (computed.CommitMap.TryGetValue(cherryPick.CommitId, out var sourceCommit))
                    {
                        var cherryId = $"cherry{commitCounter}";
                        var cherryCommit = new GitCommit
                        {
                            Id = cherryId,
                            Message = sourceCommit.Message,
                            Tag = cherryPick.Tag,
                            Type = CommitType.Normal,
                            Branch = currentBranch,
                            Row = commitCounter
                        };

                        if (branchHeads.TryGetValue(currentBranch, out var cherryHead))
                        {
                            cherryCommit.Parents.Add(cherryHead);
                        }

                        computed.Commits.Add(cherryCommit);
                        computed.CommitMap[cherryId] = cherryCommit;
                        branchHeads[currentBranch] = cherryId;

                        if (branchMap.TryGetValue(currentBranch, out var cherryBranch))
                        {
                            cherryBranch.Commits.Add(cherryCommit);
                        }

                        lastCommitId = cherryId;
                        commitCounter++;
                    }
                    break;
            }
        }

        return computed;
    }

    static double MeasureText(string text, double fontSize) =>
        text.Length * fontSize * 0.55;

    static string Fmt(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
}
