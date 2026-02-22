namespace MermaidSharp.Diagrams.GitGraph;

public class GitGraphParser : IDiagramParser<GitGraphModel>
{
    public DiagramType DiagramType => DiagramType.GitGraph;

    // Identifiers
    static Parser<char, string> BranchName =
        Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '-' || _ == '/')
            .AtLeastOnceString()
            .Labelled("branch name");

    static Parser<char, string> CommitId =
        Token(_ => char.IsLetterOrDigit(_) || _ == '_' || _ == '-')
            .AtLeastOnceString()
            .Labelled("commit id");

    // Commit type
    static Parser<char, CommitType> CommitTypeParser =
        OneOf(
            Try(CIString("REVERSE")).ThenReturn(CommitType.Reverse),
            Try(CIString("HIGHLIGHT")).ThenReturn(CommitType.Highlight),
            CIString("NORMAL").ThenReturn(CommitType.Normal)
        );

    // Attribute parsers
    static Parser<char, string> IdAttribute =>
        from _ in Try(CIString("id"))
        from __ in CommonParsers.InlineWhitespace
        from ___ in Char(':')
        from ____ in CommonParsers.InlineWhitespace
        from id in CommonParsers.QuotedString.Or(CommitId)
        select id;

    static Parser<char, string> MessageAttribute =>
        from _ in Try(CIString("msg"))
        from __ in CommonParsers.InlineWhitespace
        from ___ in Char(':')
        from ____ in CommonParsers.InlineWhitespace
        from msg in CommonParsers.QuotedString
        select msg;

    static Parser<char, string> TagAttribute =>
        from _ in Try(CIString("tag"))
        from __ in CommonParsers.InlineWhitespace
        from ___ in Char(':')
        from ____ in CommonParsers.InlineWhitespace
        from tag in CommonParsers.QuotedString
        select tag;

    static Parser<char, CommitType> TypeAttribute =>
        from _ in Try(CIString("type"))
        from __ in CommonParsers.InlineWhitespace
        from ___ in Char(':')
        from ____ in CommonParsers.InlineWhitespace
        from type in CommitTypeParser
        select type;

    static Parser<char, int> OrderAttribute =>
        from _ in Try(CIString("order"))
        from __ in CommonParsers.InlineWhitespace
        from ___ in Char(':')
        from ____ in CommonParsers.InlineWhitespace
        from order in CommonParsers.Integer
        select order;

    // Commit: commit id: "abc" msg: "message" tag: "v1.0" type: NORMAL
    static Parser<char, CommitOperation> CommitParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("commit")
        from attrs in ParseCommitAttributes()
        from ___ in CommonParsers.InlineWhitespace
        from ____ in CommonParsers.LineEnd
        select CreateCommit(attrs);

    static Parser<char, List<(string key, object value)>> ParseCommitAttributes()
    {
        var attr = OneOf(
            Try(from __ in CommonParsers.InlineWhitespace from a in IdAttribute select ("id", (object)a)),
            Try(from __ in CommonParsers.InlineWhitespace from a in MessageAttribute select ("msg", (object)a)),
            Try(from __ in CommonParsers.InlineWhitespace from a in TagAttribute select ("tag", (object)a)),
            Try(from __ in CommonParsers.InlineWhitespace from a in TypeAttribute select ("type", (object)a))
        );
        return attr.Many().Select(_ => _.ToList());
    }

    static CommitOperation CreateCommit(List<(string key, object value)> attrs)
    {
        var commit = new CommitOperation();
        foreach (var (key, value) in attrs)
        {
            switch (key)
            {
                case "id": commit.Id = (string)value; break;
                case "msg": commit.Message = (string)value; break;
                case "tag": commit.Tag = (string)value; break;
                case "type": commit.Type = (CommitType)value; break;
            }
        }
        return commit;
    }

    // Branch: branch develop order: 1
    static Parser<char, BranchOperation> BranchParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("branch")
        from ___ in CommonParsers.RequiredWhitespace
        from name in BranchName
        from order in Try(
            from ____ in CommonParsers.InlineWhitespace
            from o in OrderAttribute
            select o
        ).Optional()
        from _____ in CommonParsers.InlineWhitespace
        from ______ in CommonParsers.LineEnd
        select new BranchOperation
        {
            Name = name,
            BranchOrder = order.HasValue ? order.Value : null
        };

    // Checkout: checkout develop
    static Parser<char, CheckoutOperation> CheckoutParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("checkout")
        from ___ in CommonParsers.RequiredWhitespace
        from name in BranchName
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select new CheckoutOperation { BranchName = name };

    // Merge: merge develop id: "merge1" tag: "v1.0" type: NORMAL
    static Parser<char, MergeOperation> MergeParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("merge")
        from ___ in CommonParsers.RequiredWhitespace
        from name in BranchName
        from attrs in ParseMergeAttributes()
        from ____ in CommonParsers.InlineWhitespace
        from _____ in CommonParsers.LineEnd
        select CreateMerge(name, attrs);

    static Parser<char, List<(string key, object value)>> ParseMergeAttributes()
    {
        var attr = OneOf(
            Try(from __ in CommonParsers.InlineWhitespace from a in IdAttribute select ("id", (object)a)),
            Try(from __ in CommonParsers.InlineWhitespace from a in TagAttribute select ("tag", (object)a)),
            Try(from __ in CommonParsers.InlineWhitespace from a in TypeAttribute select ("type", (object)a))
        );
        return attr.Many().Select(_ => _.ToList());
    }

    static MergeOperation CreateMerge(string name, List<(string key, object value)> attrs)
    {
        var merge = new MergeOperation { BranchName = name };
        foreach (var (key, value) in attrs)
        {
            switch (key)
            {
                case "id": merge.Id = (string)value; break;
                case "tag": merge.Tag = (string)value; break;
                case "type": merge.Type = (CommitType)value; break;
            }
        }
        return merge;
    }

    // Cherry-pick: cherry-pick id: "abc" tag: "v1.0"
    static Parser<char, CherryPickOperation> CherryPickParser =
        from _ in CommonParsers.InlineWhitespace
        from __ in CIString("cherry-pick")
        from ___ in CommonParsers.InlineWhitespace
        from id in IdAttribute
        from tag in Try(
            from ____ in CommonParsers.InlineWhitespace
            from t in TagAttribute
            select t
        ).Optional()
        from _____ in CommonParsers.InlineWhitespace
        from ______ in CommonParsers.LineEnd
        select new CherryPickOperation
        {
            CommitId = id,
            Tag = tag.HasValue ? tag.Value : null
        };

    // Skip line (comments, empty lines)
    static Parser<char, Unit> SkipLine =
        CommonParsers.InlineWhitespace
            .Then(Try(CommonParsers.Comment).Or(CommonParsers.Newline));

    // Main content parser
    static Parser<char, List<GitOperation>> ParseContent()
    {
        var operation = OneOf(
            Try(CommitParser.Select(_ => (GitOperation?)_)),
            Try(BranchParser.Select(_ => (GitOperation?)_)),
            Try(CheckoutParser.Select(_ => (GitOperation?)_)),
            Try(MergeParser.Select(_ => (GitOperation?)_)),
            Try(CherryPickParser.Select(_ => (GitOperation?)_)),
            SkipLine.ThenReturn((GitOperation?)null)
        );

        return operation.Many()
            .Select(_ => _.Where(_ => _ != null).Cast<GitOperation>().ToList());
    }

    // Options parser (gitGraph TB: or gitGraph LR:)
    static Parser<char, (string? direction, string? mainBranch)> OptionsParser =
        from _ in CommonParsers.InlineWhitespace
        from options in Try(
            from dir in OneOf(
                Try(String("TB")).ThenReturn("TB"),
                Try(String("BT")).ThenReturn("BT"),
                Try(String("LR")).ThenReturn("LR"),
                String("RL").ThenReturn("RL")
            ).Optional()
            from __ in CommonParsers.InlineWhitespace
            from ___ in Char(':').Optional()
            select (dir.HasValue ? dir.Value : null, (string?)null)
        ).Optional()
        select options.HasValue ? options.Value : (null, null);

    public static Parser<char, GitGraphModel> Parser =>
        from _ in CommonParsers.InlineWhitespace
        from keyword in CIString("gitGraph")
        from options in OptionsParser
        from __ in CommonParsers.InlineWhitespace
        from ___ in CommonParsers.LineEnd
        from operations in ParseContent()
        select BuildModel(operations, options);

    static GitGraphModel BuildModel(List<GitOperation> operations, (string? direction, string? mainBranch) options)
    {
        var model = new GitGraphModel();

        if (options.direction != null)
        {
            model.Direction = options.direction switch
            {
                "TB" or "TD" => Direction.TopToBottom,
                "BT" => Direction.BottomToTop,
                "LR" => Direction.LeftToRight,
                "RL" => Direction.RightToLeft,
                _ => Direction.LeftToRight
            };
        }
        else
        {
            model.Direction = Direction.LeftToRight; // Git graphs default to LR
        }

        if (options.mainBranch != null)
        {
            model.MainBranchName = options.mainBranch;
        }

        var order = 0;
        foreach (var op in operations)
        {
            op.Order = order++;
            model.Operations.Add(op);
        }

        return model;
    }

    public Result<char, GitGraphModel> Parse(string input) => Parser.Parse(input);
}
