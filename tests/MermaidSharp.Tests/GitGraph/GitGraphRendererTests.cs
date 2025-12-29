public class GitGraphRendererTests : TestBase
{
    [Test]
    public Task SimpleCommits()
    {
        const string input =
            """
            gitGraph
                commit
                commit
                commit
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task CommitWithId()
    {
        const string input =
            """
            gitGraph
                commit id: "alpha"
                commit id: "beta"
                commit id: "gamma"
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task CommitWithTag()
    {
        const string input =
            """
            gitGraph
                commit
                commit tag: "v1.0.0"
                commit
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task CommitWithMessage()
    {
        const string input =
            """
            gitGraph
                commit id: "init" msg: "Initial commit"
                commit id: "feat" msg: "Add feature"
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task CommitTypes()
    {
        const string input =
            """
            gitGraph
                commit type: NORMAL
                commit type: REVERSE
                commit type: HIGHLIGHT
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task BranchAndCheckout()
    {
        const string input =
            """
            gitGraph
                commit
                branch develop
                commit
                checkout main
                commit
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task MultipleBranches()
    {
        const string input =
            """
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

        return VerifySvg(input);
    }

    [Test]
    public Task MergeBranch()
    {
        const string input =
            """
            gitGraph
                commit
                branch develop
                commit
                commit
                checkout main
                merge develop
                commit
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task MergeWithTag()
    {
        const string input =
            """
            gitGraph
                commit
                branch develop
                commit
                checkout main
                merge develop tag: "v2.0.0"
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task CherryPick()
    {
        const string input =
            """
            gitGraph
                commit id: "one"
                branch develop
                commit id: "two"
                checkout main
                cherry-pick id: "two"
            """;

        return VerifySvg(input);
    }

    [Test]
    public Task CompleteGitGraph()
    {
        const string input =
            """
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

        return VerifySvg(input);
    }
}
