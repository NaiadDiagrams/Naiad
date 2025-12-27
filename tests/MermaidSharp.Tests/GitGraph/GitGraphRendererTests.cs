using TUnit.Core;

namespace MermaidSharp.Tests.GitGraph;

public class GitGraphRendererTests
{
    [Test]
    public Task Render_SimpleCommits()
    {
        const string input = """
            gitGraph
                commit
                commit
                commit
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_CommitWithId()
    {
        const string input = """
            gitGraph
                commit id: "alpha"
                commit id: "beta"
                commit id: "gamma"
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_CommitWithTag()
    {
        const string input = """
            gitGraph
                commit
                commit tag: "v1.0.0"
                commit
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_CommitWithMessage()
    {
        const string input = """
            gitGraph
                commit id: "init" msg: "Initial commit"
                commit id: "feat" msg: "Add feature"
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_CommitTypes()
    {
        const string input = """
            gitGraph
                commit type: NORMAL
                commit type: REVERSE
                commit type: HIGHLIGHT
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_BranchAndCheckout()
    {
        const string input = """
            gitGraph
                commit
                branch develop
                commit
                checkout main
                commit
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_MultipleBranches()
    {
        const string input = """
            gitGraph
                commit
                branch develop
                commit
                branch feature
                commit
                checkout develop
                commit
                checkout main
                commit
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_MergeBranch()
    {
        const string input = """
            gitGraph
                commit
                branch develop
                commit
                commit
                checkout main
                merge develop
                commit
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_MergeWithTag()
    {
        const string input = """
            gitGraph
                commit
                branch develop
                commit
                checkout main
                merge develop tag: "v2.0.0"
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_CherryPick()
    {
        const string input = """
            gitGraph
                commit id: "one"
                branch develop
                commit id: "two"
                checkout main
                cherry-pick id: "two"
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }

    [Test]
    public Task Render_CompleteGitGraph()
    {
        const string input = """
            gitGraph
                commit id: "init" tag: "v1.0"
                branch develop
                commit id: "dev1"
                commit id: "dev2"
                branch feature
                commit id: "feat1"
                checkout develop
                merge feature
                checkout main
                merge develop tag: "v2.0"
                commit id: "hotfix" type: HIGHLIGHT
            """;

        var svg = Mermaid.Render(input);
        return Verify(svg);
    }
}
